using FluentAssertions;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Core.Wallpapers;
using Xunit;

namespace LaunchMe.Core.Tests.Wallpapers;

public sealed class WallpaperServiceTests
{
    private readonly IWallpaperService _service = new WallpaperService();

    [Fact]
    public void Resolve_StaticWallpaper_ReturnsExpectedMetadata()
    {
        var settings = new WallpaperSettings
        {
            Type = WallpaperType.Static,
            Asset = "wallpapers/nature.jpg",
            Blur = 0.25
        };

        var resolved = _service.Resolve(settings, isLauncherVisible: true);

        resolved.Type.Should().Be(WallpaperType.Static);
        resolved.Blur.Should().Be(0.25);
        resolved.PlaybackState.Should().Be(VideoPlaybackState.Disabled);
    }

    [Fact]
    public void Resolve_LiveVideoWallpaper_EnforcesPauseOnHideRule()
    {
        var settings = new WallpaperSettings
        {
            Type = WallpaperType.LiveVideo,
            Asset = "wallpapers/cyberpunk.mp4",
            Blur = 0.0,
            PauseOnHide = true
        };

        // When launcher hidden, playback state must be Paused (or Disabled if file absent on disk)
        var resolvedHidden = _service.Resolve(settings, isLauncherVisible: false);
        resolvedHidden.Type.Should().Be(WallpaperType.LiveVideo);
        resolvedHidden.PlaybackState.Should().BeOneOf(VideoPlaybackState.Paused, VideoPlaybackState.Disabled);
    }

    [Fact]
    public void IsSupportedAsset_ValidatesImageAndVideoFormats()
    {
        _service.IsSupportedAsset("test.jpg", WallpaperType.Static).Should().BeTrue();
        _service.IsSupportedAsset("test.png", WallpaperType.Static).Should().BeTrue();
        _service.IsSupportedAsset("test.mp4", WallpaperType.LiveVideo).Should().BeTrue();
        _service.IsSupportedAsset("test.mp4", WallpaperType.Static).Should().BeFalse();
        _service.IsSupportedAsset("test.jpg", WallpaperType.LiveVideo).Should().BeFalse();
    }
}
