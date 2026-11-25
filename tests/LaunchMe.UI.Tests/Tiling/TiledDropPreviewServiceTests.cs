using FluentAssertions;
using LaunchMe.Contracts.Tiling;
using LaunchMe.UI.Tiling;
using Xunit;

namespace LaunchMe.UI.Tests.Tiling;

public class TiledDropPreviewServiceTests
{
    [Fact]
    public void InitialState_IsHidden()
    {
        var service = new TiledDropPreviewService();

        service.IsVisible.Should().BeFalse();
        service.TargetBounds.Should().Be(WindowRect.Empty);
    }

    [Fact]
    public void ShowPreview_ValidBounds_SetsVisibleAndUpdatesTarget()
    {
        var service = new TiledDropPreviewService();
        var bounds = new WindowRect(50, 50, 900, 700);

        service.ShowPreview(bounds);

        service.IsVisible.Should().BeTrue();
        service.TargetBounds.Should().Be(bounds);
    }

    [Fact]
    public void ShowPreview_EmptyBounds_HidesPreview()
    {
        var service = new TiledDropPreviewService();
        service.ShowPreview(new WindowRect(50, 50, 900, 700));

        service.ShowPreview(WindowRect.Empty);

        service.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void HidePreview_ResetsVisibilityAndBounds()
    {
        var service = new TiledDropPreviewService();
        service.ShowPreview(new WindowRect(50, 50, 900, 700));

        service.HidePreview();

        service.IsVisible.Should().BeFalse();
        service.TargetBounds.Should().Be(WindowRect.Empty);
    }
}
