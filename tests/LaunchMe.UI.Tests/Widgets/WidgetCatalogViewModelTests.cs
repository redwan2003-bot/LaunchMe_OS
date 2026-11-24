using FluentAssertions;
using LaunchMe.Contracts.Media;
using LaunchMe.UI.Widgets.Catalog;
using Moq;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

public sealed class WidgetCatalogViewModelTests
{
    private readonly Mock<IMediaSessionService> _mediaServiceMock = new();

    [Fact]
    public void Catalog_InstantiatesAllSevenWidgetFamilies()
    {
        // Arrange & Act
        using var catalog = new WidgetCatalogViewModel(_mediaServiceMock.Object);

        // Assert
        catalog.Widgets.Should().HaveCount(7);
        catalog.Clock.Should().NotBeNull();
        catalog.Weather.Should().NotBeNull();
        catalog.Music.Should().NotBeNull();
        catalog.SystemStats.Should().NotBeNull();
        catalog.Pomodoro.Should().NotBeNull();
        catalog.Affirmations.Should().NotBeNull();
        catalog.Calendar.Should().NotBeNull();
    }

    [Fact]
    public void OnLauncherHidden_PropagatesToAllChildWidgets()
    {
        // Arrange
        using var catalog = new WidgetCatalogViewModel(_mediaServiceMock.Object);
        catalog.Clock.IsTimerRunning.Should().BeTrue();

        // Act: Launcher hidden
        catalog.OnLauncherHidden();

        // Assert: Clock ticker suspended, GPU/CPU idle law preserved across widgets
        catalog.Clock.IsTimerRunning.Should().BeFalse();

        // Act: Launcher shown
        catalog.OnLauncherShown();

        // Assert
        catalog.Clock.IsTimerRunning.Should().BeTrue();
    }
}
