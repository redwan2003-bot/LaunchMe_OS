using FluentAssertions;
using LaunchMe.Core.Wallpapers;
using Xunit;

namespace LaunchMe.Core.Tests.Wallpapers;

public sealed class VideoAssetResolverTests
{
    [Theory]
    [InlineData("video.mp4", true)]
    [InlineData("clip.mov", true)]
    [InlineData("anim.avi", true)]
    [InlineData("movie.mkv", true)]
    [InlineData("loop.webm", true)]
    [InlineData("image.jpg", false)]
    [InlineData("photo.png", false)]
    [InlineData("app.exe", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsSupportedVideo_ValidatesExtensions(string? path, bool expected)
    {
        var result = VideoAssetResolver.IsSupportedVideo(path);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("clip.mp4", "video/mp4")]
    [InlineData("clip.mov", "video/quicktime")]
    [InlineData("clip.avi", "video/x-msvideo")]
    [InlineData("clip.mkv", "video/x-matroska")]
    [InlineData("clip.webm", "video/webm")]
    public void DetectMimeType_ReturnsCorrectType(string path, string expectedMime)
    {
        var mime = VideoAssetResolver.DetectMimeType(path);
        mime.Should().Be(expectedMime);
    }

    [Fact]
    public void EvaluatePlaybackState_PauseOnHideWhenLauncherHidden_ReturnsPaused()
    {
        // Enforces Pause-on-Hide 0% idle GPU rule
        var state = VideoAssetResolver.EvaluatePlaybackState(
            isLauncherVisible: false,
            pauseOnHide: true,
            assetExists: true);

        state.Should().Be(VideoPlaybackState.Paused);
    }

    [Fact]
    public void EvaluatePlaybackState_WhenLauncherVisible_ReturnsPlaying()
    {
        var state = VideoAssetResolver.EvaluatePlaybackState(
            isLauncherVisible: true,
            pauseOnHide: true,
            assetExists: true);

        state.Should().Be(VideoPlaybackState.Playing);
    }

    [Fact]
    public void EvaluatePlaybackState_WhenAssetMissing_ReturnsDisabled()
    {
        var state = VideoAssetResolver.EvaluatePlaybackState(
            isLauncherVisible: true,
            pauseOnHide: true,
            assetExists: false);

        state.Should().Be(VideoPlaybackState.Disabled);
    }
}
