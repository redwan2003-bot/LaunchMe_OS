using FluentAssertions;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Contracts.Search;
using LaunchMe.Contracts.Spaces;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Wallpaper;
using Moq;
using Xunit;

namespace LaunchMe.UI.Tests.Wallpaper;

public sealed class LiveWallpaperViewModelTests
{
    private static LauncherViewModel CreateLauncherViewModel()
    {
        return new LauncherViewModel(
            new Mock<IAppIndexer>().Object,
            new Mock<IFileSearchService>().Object,
            new Mock<IClipboardMonitor>().Object,
            new Mock<ICalculatorService>().Object,
            new Mock<IEmojiService>().Object,
            new Mock<ISpaceStore>().Object);
    }

    [Fact]
    public void ApplySettings_LiveVideo_StartsPlaying_AndUpdatesProperties()
    {
        // Arrange
        using var vm = new LiveWallpaperViewModel();
        var settings = new WallpaperSettings
        {
            Type = WallpaperType.LiveVideo,
            Asset = "assets/bg.mp4",
            Blur = 0.5,
            PauseOnHide = true
        };

        // Act
        vm.ApplySettings(settings);

        // Assert
        vm.Type.Should().Be(WallpaperType.LiveVideo);
        vm.AssetPath.Should().Be("assets/bg.mp4");
        vm.BlurRadius.Should().Be(20.0); // 0.5 * 40.0
        vm.PauseOnHide.Should().BeTrue();
        vm.IsPlaying.Should().BeTrue();
        vm.ActiveGpuEstimatePercent.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void PauseOnHide_GPU_Law_ImmediatelyPausesPlayback_AndZeroesGpuLoad()
    {
        // Arrange
        using var vm = new LiveWallpaperViewModel();
        vm.ApplySettings(new WallpaperSettings
        {
            Type = WallpaperType.LiveVideo,
            Asset = "assets/loop.mp4",
            PauseOnHide = true
        });

        vm.IsPlaying.Should().BeTrue();
        vm.ActiveGpuEstimatePercent.Should().BeGreaterThan(0.0);

        // Act: Launcher hides
        vm.OnLauncherHidden();

        // Assert: GPU law enforced (0% GPU)
        vm.IsPaused.Should().BeTrue();
        vm.IsPlaying.Should().BeFalse();
        vm.ActiveGpuEstimatePercent.Should().Be(0.0);
    }

    [Fact]
    public void OnLauncherShown_ResumesPlayback_IfWasPlayingBeforeHide()
    {
        // Arrange
        using var vm = new LiveWallpaperViewModel();
        vm.ApplySettings(new WallpaperSettings
        {
            Type = WallpaperType.LiveVideo,
            Asset = "assets/loop.mp4",
            PauseOnHide = true
        });

        vm.OnLauncherHidden();
        vm.IsPaused.Should().BeTrue();

        // Act: Launcher shown again
        vm.OnLauncherShown();

        // Assert
        vm.IsPlaying.Should().BeTrue();
        vm.ActiveGpuEstimatePercent.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void AttachToLauncher_AutomaticallySynchronizesVisibilityEvents()
    {
        // Arrange
        var launcher = CreateLauncherViewModel();
        using var vm = new LiveWallpaperViewModel();
        vm.AttachToLauncher(launcher);
        vm.ApplySettings(new WallpaperSettings
        {
            Type = WallpaperType.LiveVideo,
            PauseOnHide = true
        });

        vm.IsPlaying.Should().BeTrue();

        // Act: Trigger launcher hide
        launcher.OnHidden();

        // Assert: ViewModel immediately paused via event
        vm.IsPaused.Should().BeTrue();
        vm.ActiveGpuEstimatePercent.Should().Be(0.0);

        // Act: Trigger launcher show
        launcher.OnShown();

        // Assert: ViewModel resumed
        vm.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void StaticWallpaper_DoesNotAutoPlay()
    {
        // Arrange
        using var vm = new LiveWallpaperViewModel();
        var settings = new WallpaperSettings
        {
            Type = WallpaperType.Static,
            Asset = "assets/photo.jpg"
        };

        // Act
        vm.ApplySettings(settings);

        // Assert
        vm.CurrentState.Should().Be(WallpaperPlaybackState.Stopped);
        vm.IsPlaying.Should().BeFalse();
        vm.ActiveGpuEstimatePercent.Should().Be(0.0);
    }
}
