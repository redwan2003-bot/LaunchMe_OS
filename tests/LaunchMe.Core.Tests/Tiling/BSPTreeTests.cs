using FluentAssertions;
using LaunchMe.Contracts.Tiling;
using LaunchMe.Core.Tiling;
using Xunit;

namespace LaunchMe.Core.Tests.Tiling;

public class BSPTreeTests
{
    [Fact]
    public void EmptyTree_ComputesEmptyLayout()
    {
        var tree = new BSPTree();
        var config = new TilingConfig();
        var area = new WindowRect(0, 0, 1920, 1080);

        var layout = tree.ComputeLayout(area, config);

        layout.Should().BeEmpty();
    }

    [Fact]
    public void SingleWindow_OccupiesFullAreaMinusOuterGaps()
    {
        var tree = new BSPTree();
        var config = new TilingConfig { OuterGap = 10, InnerGap = 6 };
        var win = new TiledWindow(1001, "notepad", "Notes", WindowRect.Empty, false, false, 1);

        tree.Insert(win);
        var layout = tree.ComputeLayout(new WindowRect(0, 0, 1920, 1080), config);

        layout.Should().HaveCount(1);
        layout[0].Handle.Should().Be(1001);
        layout[0].TargetRect.X.Should().Be(10);
        layout[0].TargetRect.Y.Should().Be(10);
        layout[0].TargetRect.Width.Should().Be(1900);
        layout[0].TargetRect.Height.Should().Be(1060);
    }

    [Fact]
    public void TwoWindows_WideScreen_SplitsVertically()
    {
        var tree = new BSPTree();
        var config = new TilingConfig { OuterGap = 0, InnerGap = 10, DefaultSplitRatio = 0.5 };
        var win1 = new TiledWindow(1001, "app1", "One", WindowRect.Empty, false, false, 1);
        var win2 = new TiledWindow(1002, "app2", "Two", WindowRect.Empty, false, false, 1);

        tree.Insert(win1);
        tree.Insert(win2);

        var layout = tree.ComputeLayout(new WindowRect(0, 0, 1000, 600), config);

        layout.Should().HaveCount(2);
        // Vertical split (Left and Right)
        layout[0].Handle.Should().Be(1001);
        layout[0].TargetRect.Width.Should().Be(495);
        layout[1].Handle.Should().Be(1002);
        layout[1].TargetRect.Width.Should().Be(495);
        layout[1].TargetRect.X.Should().Be(505); // 495 + 10 gap
    }

    [Fact]
    public void ThreeWindows_ProducesSpiralDwindle()
    {
        var tree = new BSPTree();
        var config = new TilingConfig { OuterGap = 0, InnerGap = 0, DefaultSplitRatio = 0.5 };
        var win1 = new TiledWindow(1001, "app1", "One", WindowRect.Empty, false, false, 1);
        var win2 = new TiledWindow(1002, "app2", "Two", WindowRect.Empty, false, false, 1);
        var win3 = new TiledWindow(1003, "app3", "Three", WindowRect.Empty, false, false, 1);

        tree.Insert(win1);
        tree.Insert(win2);
        tree.Insert(win3);

        var layout = tree.ComputeLayout(new WindowRect(0, 0, 1000, 800), config);

        layout.Should().HaveCount(3);
        layout.Select(l => l.Handle).Should().Contain(new[] { (nint)1001, (nint)1002, (nint)1003 });
    }

    [Fact]
    public void RemoveWindow_CollapsesTreeCleanly()
    {
        var tree = new BSPTree();
        var config = new TilingConfig { OuterGap = 0, InnerGap = 0 };
        var win1 = new TiledWindow(1001, "app1", "One", WindowRect.Empty, false, false, 1);
        var win2 = new TiledWindow(1002, "app2", "Two", WindowRect.Empty, false, false, 1);

        tree.Insert(win1);
        tree.Insert(win2);
        tree.Remove(1002);

        var layout = tree.ComputeLayout(new WindowRect(0, 0, 1000, 600), config);

        layout.Should().HaveCount(1);
        layout[0].Handle.Should().Be(1001);
        layout[0].TargetRect.Width.Should().Be(1000);
    }

    [Fact]
    public void Swap_ExchangesWindowsInLeaves()
    {
        var tree = new BSPTree();
        var config = new TilingConfig { OuterGap = 0, InnerGap = 0 };
        var win1 = new TiledWindow(1001, "app1", "One", WindowRect.Empty, false, false, 1);
        var win2 = new TiledWindow(1002, "app2", "Two", WindowRect.Empty, false, false, 1);

        tree.Insert(win1);
        tree.Insert(win2);
        tree.Swap(1001, 1002);

        var layout = tree.ComputeLayout(new WindowRect(0, 0, 1000, 600), config);

        layout[0].Handle.Should().Be(1002);
        layout[1].Handle.Should().Be(1001);
    }
}
