using FluentAssertions;
using LaunchMe.Contracts.Media;
using LaunchMe.UI.Widgets.Music;
using Moq;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

public sealed class MusicWidgetViewModelTests
{
    private readonly Mock<IMediaSessionService> _mediaServiceMock = new();

    [Fact]
    public void UpdateFromTrack_SetsMetadataAndProgressAccurately()
    {
        // Arrange
        using var vm = new MusicWidgetViewModel(_mediaServiceMock.Object, startActive: false);
        var track = new MediaTrackInfo
        {
            Title = "Midnight City",
            Artist = "M83",
            AlbumTitle = "Hurry Up, We're Dreaming",
            Duration = TimeSpan.FromMinutes(4),
            Position = TimeSpan.FromMinutes(1),
            IsPlaying = true
        };

        // Act
        vm.UpdateFromTrack(track);

        // Assert
        vm.TrackTitle.Should().Be("Midnight City");
        vm.Artist.Should().Be("M83");
        vm.Album.Should().Be("Hurry Up, We're Dreaming");
        vm.PositionFormatted.Should().Be("01:00");
        vm.DurationFormatted.Should().Be("04:00");
        vm.ProgressRatio.Should().Be(0.25); // 1 min / 4 min
        vm.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void PlayPauseCommand_DelegatesToMediaSessionService()
    {
        // Arrange
        using var vm = new MusicWidgetViewModel(_mediaServiceMock.Object, startActive: false);

        // Act
        vm.PlayPauseCommand.Execute(null);

        // Assert
        _mediaServiceMock.Verify(x => x.PlayPauseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void NextAndPreviousCommands_DelegateToMediaSessionService()
    {
        // Arrange
        using var vm = new MusicWidgetViewModel(_mediaServiceMock.Object, startActive: false);

        // Act
        vm.NextCommand.Execute(null);
        vm.PreviousCommand.Execute(null);

        // Assert
        _mediaServiceMock.Verify(x => x.NextAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mediaServiceMock.Verify(x => x.PreviousAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
