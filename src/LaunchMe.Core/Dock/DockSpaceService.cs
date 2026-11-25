using System.Text.Json;
using LaunchMe.Contracts.Dock;

namespace LaunchMe.Core.Dock;

public sealed class DockSpaceService : IDockSpaceService
{
    private readonly string _storagePath;
    private readonly List<DockSpaceModel> _spaces = [];
    private readonly Dictionary<string, string> _activeSpacePerDisplay = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private bool _disposed;

    public event EventHandler? DockSpacesChanged;

    public DockSpaceService(string? storagePath = null)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _storagePath = Path.Combine(localAppData, "LaunchMe", "dock_spaces.json");
        }
        else
        {
            _storagePath = storagePath;
        }

        LoadFromDisk();
    }

    public IReadOnlyList<DockSpaceModel> GetDockSpaces()
    {
        lock (_lock)
        {
            return _spaces.ToList();
        }
    }

    public DockSpaceModel? GetActiveDockSpace(string displayId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayId);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_activeSpacePerDisplay.TryGetValue(displayId, out var activeId))
            {
                var match = _spaces.FirstOrDefault(s => s.Id.Equals(activeId, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }

            var displaySpaces = _spaces.Where(s => s.DisplayId.Equals(displayId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (displaySpaces.Count > 0)
            {
                _activeSpacePerDisplay[displayId] = displaySpaces[0].Id;
                return displaySpaces[0];
            }

            // Create default dock space for this display
            var defaultSpace = new DockSpaceModel
            {
                Id = Guid.NewGuid().ToString(),
                DisplayId = displayId,
                Name = $"Dock Space 1 ({displayId})",
                Items =
                [
                    new DockItem { Title = "File Explorer", TargetPath = "explorer.exe", IconPath = "explorer.exe" },
                    new DockItem { Title = "Terminal", TargetPath = "wt.exe", IconPath = "wt.exe" }
                ],
                ActiveWidgets = ["SystemStats", "MusicCompact", "ToolBar"]
            };

            _spaces.Add(defaultSpace);
            _activeSpacePerDisplay[displayId] = defaultSpace.Id;
            SaveToDisk();
            return defaultSpace;
        }
    }

    public void SetActiveDockSpace(string displayId, string dockSpaceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dockSpaceId);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _activeSpacePerDisplay[displayId] = dockSpaceId;
        }

        DockSpacesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SaveDockSpace(DockSpaceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            int index = _spaces.FindIndex(s => s.Id.Equals(model.Id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _spaces[index] = model;
            }
            else
            {
                _spaces.Add(model);
            }

            SaveToDisk();
        }

        DockSpacesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NextDockSpace(string displayId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayId);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            var displaySpaces = _spaces.Where(s => s.DisplayId.Equals(displayId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (displaySpaces.Count <= 1) return;

            string currentId = _activeSpacePerDisplay.TryGetValue(displayId, out var id) ? id : displaySpaces[0].Id;
            int currentIndex = displaySpaces.FindIndex(s => s.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase));
            int nextIndex = (currentIndex + 1) % displaySpaces.Count;

            _activeSpacePerDisplay[displayId] = displaySpaces[nextIndex].Id;
        }

        DockSpacesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void PreviousDockSpace(string displayId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayId);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            var displaySpaces = _spaces.Where(s => s.DisplayId.Equals(displayId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (displaySpaces.Count <= 1) return;

            string currentId = _activeSpacePerDisplay.TryGetValue(displayId, out var id) ? id : displaySpaces[0].Id;
            int currentIndex = displaySpaces.FindIndex(s => s.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase));
            int prevIndex = (currentIndex - 1 + displaySpaces.Count) % displaySpaces.Count;

            _activeSpacePerDisplay[displayId] = displaySpaces[prevIndex].Id;
        }

        DockSpacesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadFromDisk()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    string json = File.ReadAllText(_storagePath);
                    var loaded = JsonSerializer.Deserialize<List<DockSpaceModel>>(json);
                    if (loaded != null && loaded.Count > 0)
                    {
                        _spaces.Clear();
                        _spaces.AddRange(loaded);
                    }
                }
            }
            catch
            {
            }
        }
    }

    private void SaveToDisk()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(_spaces, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storagePath, json);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
