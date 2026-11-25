using LaunchMe.Contracts.Tiling;

namespace LaunchMe.Core.Tiling;

/// <summary>
/// Implements Squish's Dwindle spiral layout algorithm.
/// </summary>
public static class DwindleLayout
{
    /// <summary>
    /// Recursively computes target window rectangles across the BSP tree.
    /// </summary>
    public static void Partition(BSPNode node, WindowRect area, TilingConfig config, List<(nint Handle, WindowRect TargetRect)> results)
    {
        node.Bounds = area;

        if (node.IsLeaf)
        {
            if (node.Window != null)
            {
                results.Add((node.Window.Handle, area));
            }
            return;
        }

        if (node.Left == null || node.Right == null)
        {
            return;
        }

        double ratio = Math.Clamp(node.SplitRatio, config.MinSplitRatio, config.MaxSplitRatio);
        int gap = Math.Max(0, config.InnerGap);

        if (node.Axis == SplitAxis.Vertical)
        {
            // Left and Right split
            int totalWidth = Math.Max(0, area.Width - gap);
            int leftWidth = (int)Math.Round(totalWidth * ratio);
            int rightWidth = Math.Max(0, totalWidth - leftWidth);

            var leftArea = new WindowRect(area.X, area.Y, leftWidth, area.Height);
            var rightArea = new WindowRect(area.X + leftWidth + gap, area.Y, rightWidth, area.Height);

            Partition(node.Left, leftArea, config, results);
            Partition(node.Right, rightArea, config, results);
        }
        else
        {
            // Top and Bottom split
            int totalHeight = Math.Max(0, area.Height - gap);
            int topHeight = (int)Math.Round(totalHeight * ratio);
            int bottomHeight = Math.Max(0, totalHeight - topHeight);

            var topArea = new WindowRect(area.X, area.Y, area.Width, topHeight);
            var bottomArea = new WindowRect(area.X, area.Y + topHeight + gap, area.Width, bottomHeight);

            Partition(node.Left, topArea, config, results);
            Partition(node.Right, bottomArea, config, results);
        }
    }
}
