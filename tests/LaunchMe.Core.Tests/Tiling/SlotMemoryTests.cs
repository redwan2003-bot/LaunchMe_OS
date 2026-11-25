using FluentAssertions;
using LaunchMe.Contracts.Tiling;
using LaunchMe.Core.Tiling;
using Xunit;

namespace LaunchMe.Core.Tests.Tiling;

public class SlotMemoryTests
{
    [Fact]
    public void Remember_And_Recall_ReturnsMatchingRecord()
    {
        var slots = new SlotMemory();
        slots.Remember(1001, 1002, SplitDirection.Right, SplitAxis.Vertical, 0.60);

        var record = slots.Recall(1001);

        record.Should().NotBeNull();
        record!.PartnerHwnd.Should().Be(1002);
        record.Side.Should().Be(SplitDirection.Right);
        record.Axis.Should().Be(SplitAxis.Vertical);
        record.Ratio.Should().Be(0.60);
    }

    [Fact]
    public void Forget_RemovesSlotRecord()
    {
        var slots = new SlotMemory();
        slots.Remember(1001, 1002, SplitDirection.Left, SplitAxis.Horizontal, 0.50);
        slots.Forget(1001);

        slots.Recall(1001).Should().BeNull();
    }

    [Fact]
    public void TreeRemoval_And_Reinsertion_UsesSlotMemory()
    {
        var tree = new BSPTree();
        var config = new TilingConfig { OuterGap = 0, InnerGap = 0 };
        var win1 = new TiledWindow(1001, "app1", "One", WindowRect.Empty, false, false, 1);
        var win2 = new TiledWindow(1002, "app2", "Two", WindowRect.Empty, false, false, 1);

        tree.Insert(win1);
        tree.Insert(win2);
        tree.Remove(1002);

        // When win2 returns, it should remember it was paired with win1
        tree.Insert(win2);

        var layout = tree.ComputeLayout(new WindowRect(0, 0, 1000, 600), config);
        layout.Should().HaveCount(2);
        layout[0].Handle.Should().Be(1001);
        layout[1].Handle.Should().Be(1002);
    }
}
