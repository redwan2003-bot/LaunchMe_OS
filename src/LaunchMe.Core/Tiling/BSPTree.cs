using LaunchMe.Contracts.Tiling;

namespace LaunchMe.Core.Tiling;

/// <summary>
/// A Binary Space Partitioning tree representing a single workspace screen layout.
/// </summary>
public class BSPTree
{
    public BSPNode? Root { get; private set; }
    public SlotMemory Slots { get; } = new();

    public bool IsEmpty => Root == null;

    public void Insert(TiledWindow window, nint? focusTarget = null)
    {
        var newLeaf = BSPNode.CreateLeaf(window);

        if (Root == null)
        {
            Root = newLeaf;
            return;
        }

        // Check if window has slot memory with a partner still in the tree
        var slot = Slots.Recall(window.Handle);
        BSPNode? targetLeaf = null;

        if (slot != null)
        {
            targetLeaf = Root.FindLeaf(slot.PartnerHwnd);
        }

        if (targetLeaf == null && focusTarget.HasValue)
        {
            targetLeaf = Root.FindLeaf(focusTarget.Value);
        }

        // Fallback: pick deepest right leaf
        targetLeaf ??= Root.GetLeaves().LastOrDefault();

        if (targetLeaf == null)
        {
            Root = newLeaf;
            return;
        }

        SplitAxis chosenAxis;
        if (slot != null)
        {
            chosenAxis = slot.Axis;
        }
        else
        {
            // Dwindle rule: split longer dimension of target node bounds
            chosenAxis = targetLeaf.Bounds.Width >= targetLeaf.Bounds.Height
                ? SplitAxis.Vertical
                : SplitAxis.Horizontal;
        }

        double ratio = slot?.Ratio ?? 0.50;
        var parent = targetLeaf.Parent;

        BSPNode splitNode;
        if (slot != null && (slot.Side == SplitDirection.Left || slot.Side == SplitDirection.Up))
        {
            splitNode = BSPNode.CreateInternal(newLeaf, targetLeaf, chosenAxis, ratio);
        }
        else
        {
            splitNode = BSPNode.CreateInternal(targetLeaf, newLeaf, chosenAxis, ratio);
        }

        if (parent == null)
        {
            Root = splitNode;
        }
        else
        {
            splitNode.Parent = parent;
            if (parent.Left == targetLeaf)
            {
                parent.Left = splitNode;
            }
            else
            {
                parent.Right = splitNode;
            }
        }
    }

    public bool Remove(nint hwnd)
    {
        if (Root == null)
        {
            return false;
        }

        var leaf = Root.FindLeaf(hwnd);
        if (leaf == null)
        {
            return false;
        }

        var parent = leaf.Parent;
        if (parent == null)
        {
            // Only node in tree
            Root = null;
            return true;
        }

        var sibling = parent.Left == leaf ? parent.Right : parent.Left;
        if (sibling == null)
        {
            Root = null;
            return true;
        }

        // Record slot memory if sibling is a leaf with window
        if (sibling.Window != null)
        {
            var side = parent.Left == leaf
                ? (parent.Axis == SplitAxis.Vertical ? SplitDirection.Left : SplitDirection.Up)
                : (parent.Axis == SplitAxis.Vertical ? SplitDirection.Right : SplitDirection.Down);

            Slots.Remember(hwnd, sibling.Window.Handle, side, parent.Axis, parent.SplitRatio);
        }

        var grandParent = parent.Parent;
        sibling.Parent = grandParent;

        if (grandParent == null)
        {
            Root = sibling;
        }
        else
        {
            if (grandParent.Left == parent)
            {
                grandParent.Left = sibling;
            }
            else
            {
                grandParent.Right = sibling;
            }
        }

        return true;
    }

    public void ToggleSplitAxis(nint hwnd)
    {
        var leaf = Root?.FindLeaf(hwnd);
        if (leaf?.Parent != null)
        {
            leaf.Parent.Axis = leaf.Parent.Axis == SplitAxis.Horizontal
                ? SplitAxis.Vertical
                : SplitAxis.Horizontal;
        }
    }

    public void Swap(nint hwnd1, nint hwnd2)
    {
        var leaf1 = Root?.FindLeaf(hwnd1);
        var leaf2 = Root?.FindLeaf(hwnd2);

        if (leaf1 != null && leaf2 != null && leaf1 != leaf2)
        {
            (leaf1.Window, leaf2.Window) = (leaf2.Window, leaf1.Window);
        }
    }

    public IReadOnlyList<(nint Handle, WindowRect TargetRect)> ComputeLayout(WindowRect availableArea, TilingConfig config)
    {
        var results = new List<(nint Handle, WindowRect TargetRect)>();
        if (Root == null)
        {
            return results;
        }

        var innerArea = availableArea.Inset(config.OuterGap, config.OuterGap);
        DwindleLayout.Partition(Root, innerArea, config, results);
        return results;
    }
}
