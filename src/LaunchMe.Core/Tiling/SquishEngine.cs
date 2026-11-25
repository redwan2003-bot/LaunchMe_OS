using LaunchMe.Contracts.Tiling;

namespace LaunchMe.Core.Tiling;

/// <summary>
/// Squish dynamic BSP tiling window engine ("Squish") for Windows 11.
/// </summary>
public class SquishEngine : ITilingEngine
{
    private readonly Dictionary<(string DisplayId, int WorkspaceId), BSPTree> _trees = new();
    private readonly Dictionary<nint, TiledWindow> _windows = new();
    private readonly HashSet<nint> _floatingWindows = new();
    private readonly HashSet<nint> _scratchpadWindows = new();
    private readonly object _lock = new();

    private nint? _focusedWindow;

    public TilingConfig Config { get; set; } = new();

    public void RegisterWindow(nint hwnd, string processName, string title, WindowRect initialBounds, int workspaceId)
    {
        lock (_lock)
        {
            var window = new TiledWindow(
                hwnd,
                processName,
                title,
                initialBounds,
                IsFloating: _floatingWindows.Contains(hwnd),
                IsPinnedToScratchpad: _scratchpadWindows.Contains(hwnd),
                workspaceId
            );

            _windows[hwnd] = window;
            _focusedWindow = hwnd;

            if (!window.IsFloating && !window.IsPinnedToScratchpad)
            {
                var tree = GetOrCreateTree("primary", workspaceId);
                tree.Insert(window, _focusedWindow);
            }
        }
    }

    public void UnregisterWindow(nint hwnd)
    {
        lock (_lock)
        {
            if (_windows.TryGetValue(hwnd, out var window))
            {
                var tree = GetOrCreateTree("primary", window.WorkspaceId);
                tree.Remove(hwnd);
                _windows.Remove(hwnd);
                _floatingWindows.Remove(hwnd);
                _scratchpadWindows.Remove(hwnd);

                if (_focusedWindow == hwnd)
                {
                    _focusedWindow = tree.Root?.GetLeaves().FirstOrDefault()?.Window?.Handle;
                }
            }
        }
    }

    public void FocusWindow(nint hwnd)
    {
        lock (_lock)
        {
            if (_windows.ContainsKey(hwnd))
            {
                _focusedWindow = hwnd;
            }
        }
    }

    public nint? GetFocusedWindow()
    {
        lock (_lock)
        {
            return _focusedWindow;
        }
    }

    public nint? FindDirectionalTarget(nint currentHwnd, SplitDirection direction)
    {
        lock (_lock)
        {
            if (!_windows.TryGetValue(currentHwnd, out var current))
            {
                return null;
            }

            var tree = GetOrCreateTree("primary", current.WorkspaceId);
            var candidates = tree.Root?.GetLeaves()
                .Where(l => l.Window != null && l.Window.Handle != currentHwnd)
                .Select(l => (Leaf: l, Rect: l.Bounds))
                .ToList();

            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var srcRect = current.Frame;
            var srcCx = srcRect.CenterX;
            var srcCy = srcRect.CenterY;

            nint? bestTarget = null;
            double bestScore = double.MaxValue;

            foreach (var (leaf, rect) in candidates)
            {
                var tgtCx = rect.CenterX;
                var tgtCy = rect.CenterY;

                var dx = tgtCx - srcCx;
                var dy = tgtCy - srcCy;

                bool isValidDirection = direction switch
                {
                    SplitDirection.Left => dx < -10,
                    SplitDirection.Right => dx > 10,
                    SplitDirection.Up => dy < -10,
                    SplitDirection.Down => dy > 10,
                    _ => false
                };

                if (!isValidDirection)
                {
                    continue;
                }

                double distance = direction is SplitDirection.Left or SplitDirection.Right
                    ? Math.Abs(dx) + (Math.Abs(dy) * 1.5)
                    : (Math.Abs(dx) * 1.5) + Math.Abs(dy);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestTarget = leaf.Window?.Handle;
                }
            }

            return bestTarget;
        }
    }

    public void SwapWindow(nint hwnd, SplitDirection direction)
    {
        lock (_lock)
        {
            var target = FindDirectionalTarget(hwnd, direction);
            if (target.HasValue && _windows.TryGetValue(hwnd, out var win))
            {
                var tree = GetOrCreateTree("primary", win.WorkspaceId);
                tree.Swap(hwnd, target.Value);
            }
        }
    }

    public void ToggleSplitAxis(nint hwnd)
    {
        lock (_lock)
        {
            if (_windows.TryGetValue(hwnd, out var win))
            {
                var tree = GetOrCreateTree("primary", win.WorkspaceId);
                tree.ToggleSplitAxis(hwnd);
            }
        }
    }

    public void ToggleFloating(nint hwnd)
    {
        lock (_lock)
        {
            if (!_windows.TryGetValue(hwnd, out var win))
            {
                return;
            }

            var tree = GetOrCreateTree("primary", win.WorkspaceId);
            if (_floatingWindows.Contains(hwnd))
            {
                _floatingWindows.Remove(hwnd);
                _windows[hwnd] = win with { IsFloating = false };
                tree.Insert(_windows[hwnd], _focusedWindow);
            }
            else
            {
                _floatingWindows.Add(hwnd);
                _windows[hwnd] = win with { IsFloating = true };
                tree.Remove(hwnd);
            }
        }
    }

    public void ToggleScratchpad(nint hwnd)
    {
        lock (_lock)
        {
            if (!_windows.TryGetValue(hwnd, out var win))
            {
                return;
            }

            var tree = GetOrCreateTree("primary", win.WorkspaceId);
            if (_scratchpadWindows.Contains(hwnd))
            {
                _scratchpadWindows.Remove(hwnd);
                _windows[hwnd] = win with { IsPinnedToScratchpad = false };
                tree.Insert(_windows[hwnd], _focusedWindow);
            }
            else
            {
                _scratchpadWindows.Add(hwnd);
                _windows[hwnd] = win with { IsPinnedToScratchpad = true };
                tree.Remove(hwnd);
            }
        }
    }

    public bool IsFloating(nint hwnd)
    {
        lock (_lock)
        {
            return _floatingWindows.Contains(hwnd);
        }
    }

    public bool IsScratchpad(nint hwnd)
    {
        lock (_lock)
        {
            return _scratchpadWindows.Contains(hwnd);
        }
    }

    public IReadOnlyList<(nint Handle, WindowRect TargetRect)> ComputeLayout(string displayId, WindowRect availableArea, int workspaceId)
    {
        lock (_lock)
        {
            var tree = GetOrCreateTree(displayId, workspaceId);
            var layout = tree.ComputeLayout(availableArea, Config);

            foreach (var (handle, rect) in layout)
            {
                if (_windows.TryGetValue(handle, out var win))
                {
                    _windows[handle] = win with { Frame = rect };
                }
            }

            return layout;
        }
    }

    public IReadOnlyList<TiledWindow> GetWindows(int workspaceId)
    {
        lock (_lock)
        {
            return _windows.Values.Where(w => w.WorkspaceId == workspaceId).ToList();
        }
    }

    private BSPTree GetOrCreateTree(string displayId, int workspaceId)
    {
        var key = (displayId, workspaceId);
        if (!_trees.TryGetValue(key, out var tree))
        {
            tree = new BSPTree();
            _trees[key] = tree;
        }
        return tree;
    }
}
