using FluentAssertions;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.UI.ViewModels;
using Xunit;

namespace LaunchMe.UI.Tests;

public sealed class NotchViewModelLuxuryTests : IDisposable
{
    private readonly NotchViewModel _vm = new();

    public void Dispose() => _vm.Dispose();

    [Fact]
    public void FocusMode_ToggleUpdatesStateVisibilityAndEvent()
    {
        bool? raised = null;
        _vm.FocusModeChanged += (_, active) => raised = active;

        _vm.FocusModeActive.Should().BeFalse();
        _vm.FocusModeBadgeText.Should().Be("OFF");

        _vm.ToggleFocusMode();

        _vm.FocusModeActive.Should().BeTrue();
        _vm.FocusModeBadgeText.Should().Be("FOCUS");
        raised.Should().BeTrue();
    }

    [Fact]
    public void AdjustableSize_UpdatesCompactAndExpandedDimensionsWithinBounds()
    {
        _vm.CompactWidthPx = 460;
        _vm.CompactHeightPx = 44;
        _vm.WidthPx.Should().Be(460);
        _vm.HeightPx.Should().Be(44);

        _vm.IsExpanded = true;
        _vm.ExpandedWidthPx = 680;
        _vm.ExpandedHeightPx = 480;
        _vm.WidthPx.Should().Be(680);
        _vm.HeightPx.Should().Be(480);

        _vm.ResetDimensions();
        _vm.WidthPx.Should().Be(640);
        _vm.HeightPx.Should().Be(180);

        _vm.IsExpanded = false;
        _vm.WidthPx.Should().Be(420);
        _vm.HeightPx.Should().Be(36);
    }

    [Fact]
    public void ClipboardCards_FilterByCategoryAndFavorites()
    {
        _vm.AddClipboardEntry(new ClipboardEntry { Id = "c1", Category = ClipboardCategory.Code, TextContent = "var x = 1;" });
        _vm.AddClipboardEntry(new ClipboardEntry { Id = "c2", Category = ClipboardCategory.Colors, TextContent = "#00F0FF" });
        _vm.AddClipboardEntry(new ClipboardEntry { Id = "c3", Category = ClipboardCategory.Text, TextContent = "Hello LaunchMe" });

        _vm.FilteredClipboardCards.Should().HaveCount(3);

        _vm.SelectClipboardCategory(ClipboardCategory.Code);
        _vm.FilteredClipboardCards.Should().ContainSingle(c => c.Id == "c1");

        _vm.ToggleFavoriteClipboardEntry("c2");
        _vm.SelectClipboardCategory(ClipboardCategory.Favorites);
        _vm.FilteredClipboardCards.Should().ContainSingle(c => c.Id == "c2");
    }

    [Fact]
    public void DropZones_DragOverExpandsNotchAndNearbyShareRaisesEvent()
    {
        IReadOnlyList<string>? shared = null;
        _vm.NearbyShareRequested += (_, files) => shared = files;

        _vm.BeginFileDragOver();
        _vm.IsFileDragOver.Should().BeTrue();
        _vm.IsExpanded.Should().BeTrue();

        _vm.DropFilesToNearbyShare(["C:\\temp\\a.png", "C:\\temp\\b.pdf"]);
        _vm.IsFileDragOver.Should().BeFalse();
        shared.Should().NotBeNull();
        shared!.Should().HaveCount(2);
    }
}
