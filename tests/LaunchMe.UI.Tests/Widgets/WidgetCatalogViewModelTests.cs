using FluentAssertions;
using LaunchMe.Contracts.Media;
using LaunchMe.UI.Widgets.Catalog;
using LaunchMe.UI.Widgets.Models;
using Moq;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

public sealed class WidgetCatalogViewModelTests
{
    private readonly Mock<IMediaSessionService> _mediaServiceMock = new();

    [Fact]
    public void Catalog_InstantiatesAllElevenWidgetFamilies()
    {
        // Arrange & Act
        using var catalog = new WidgetCatalogViewModel(_mediaServiceMock.Object);

        // Assert
        catalog.Widgets.Should().HaveCount(11);
        catalog.Clock.Should().NotBeNull();
        catalog.Weather.Should().NotBeNull();
        catalog.Music.Should().NotBeNull();
        catalog.SystemStats.Should().NotBeNull();
        catalog.Pomodoro.Should().NotBeNull();
        catalog.Affirmations.Should().NotBeNull();
        catalog.Calendar.Should().NotBeNull();
        catalog.PixelArt.Should().NotBeNull();
        catalog.Photo.Should().NotBeNull();
        catalog.ShapePhoto.Should().NotBeNull();
        catalog.Gif.Should().NotBeNull();

        catalog.Widgets.Select(w => w.Family).Should().Contain(
        [
            WidgetFamily.Clock,
            WidgetFamily.Weather,
            WidgetFamily.Music,
            WidgetFamily.SystemStats,
            WidgetFamily.Pomodoro,
            WidgetFamily.Affirmations,
            WidgetFamily.Calendar,
            WidgetFamily.PixelArt,
            WidgetFamily.Photo,
            WidgetFamily.ShapePhoto,
            WidgetFamily.Gif
        ]);
    }

    [Fact]
    public void OnLauncherHidden_PropagatesToAllChildWidgets()
    {
        // Arrange
        using var catalog = new WidgetCatalogViewModel(_mediaServiceMock.Object);
        catalog.Gif.GifPath = "demo.gif";
        catalog.Clock.IsTimerRunning.Should().BeTrue();
        catalog.PixelArt.IsTimerRunning.Should().BeTrue();
        catalog.Gif.IsPlaying.Should().BeTrue();

        // Act: Launcher hidden
        catalog.OnLauncherHidden();

        // Assert: Timers and GIF playback suspended, GPU/CPU idle law preserved across widgets
        catalog.Clock.IsTimerRunning.Should().BeFalse();
        catalog.PixelArt.IsTimerRunning.Should().BeFalse();
        catalog.Gif.IsPlaying.Should().BeFalse();

        // Act: Launcher shown
        catalog.OnLauncherShown();

        // Assert
        catalog.Clock.IsTimerRunning.Should().BeTrue();
        catalog.PixelArt.IsTimerRunning.Should().BeTrue();
        catalog.Gif.IsPlaying.Should().BeTrue();
    }
}
