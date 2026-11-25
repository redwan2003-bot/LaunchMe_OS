using LaunchMe.Contracts.Tiling;

namespace LaunchMe.Core.Tiling;

/// <summary>
/// A node in the Binary Space Partitioning tree.
/// Either a Leaf holding a TiledWindow, or an Internal Node with Left and Right children.
/// </summary>
public class BSPNode
{
    private double _splitRatio = 0.50;

    public BSPNode? Parent { get; set; }
    public BSPNode? Left { get; set; }
    public BSPNode? Right { get; set; }
    public TiledWindow? Window { get; set; }
    public SplitAxis Axis { get; set; } = SplitAxis.Vertical;
    public WindowRect Bounds { get; set; } = WindowRect.Empty;

    public bool IsLeaf => Window != null || (Left == null && Right == null);

    public double SplitRatio
    {
        get => _splitRatio;
        set => _splitRatio = Math.Clamp(value, 0.15, 0.85);
    }

    public static BSPNode CreateLeaf(TiledWindow window) => new() { Window = window };

    public static BSPNode CreateInternal(BSPNode left, BSPNode right, SplitAxis axis, double ratio = 0.50)
    {
        var node = new BSPNode
        {
            Axis = axis,
            SplitRatio = ratio,
            Left = left,
            Right = right
        };
        left.Parent = node;
        right.Parent = node;
        return node;
    }

    public IEnumerable<BSPNode> GetLeaves()
    {
        if (IsLeaf)
        {
            if (Window != null)
            {
                yield return this;
            }
            yield break;
        }

        if (Left != null)
        {
            foreach (var leaf in Left.GetLeaves())
            {
                yield return leaf;
            }
        }

        if (Right != null)
        {
            foreach (var leaf in Right.GetLeaves())
            {
                yield return leaf;
            }
        }
    }

    public BSPNode? FindLeaf(nint hwnd)
    {
        if (IsLeaf)
        {
            return Window?.Handle == hwnd ? this : null;
        }

        return Left?.FindLeaf(hwnd) ?? Right?.FindLeaf(hwnd);
    }
}
