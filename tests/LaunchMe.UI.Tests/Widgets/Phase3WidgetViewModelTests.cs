using FluentAssertions;
using LaunchMe.Contracts.Media;
using LaunchMe.UI.Widgets.Catalog;
using LaunchMe.UI.Widgets.Gif;
using LaunchMe.UI.Widgets.Models;
using LaunchMe.UI.Widgets.Photo;
using LaunchMe.UI.Widgets.PixelArt;
using LaunchMe.UI.Widgets.ShapePhoto;
using Microsoft.UI.Xaml;
using Moq;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

public sealed class Phase3WidgetViewModelTests
{
    [Fact]
    public void PixelArt_ExposesEighteenPresets_AndCyclesCleanly()
    {
        using var vm = new PixelArtWidgetViewModel(startActive: false);

        vm.Family.Should().Be(WidgetFamily.PixelArt);
        vm.Presets.Should().HaveCount(18);
        vm.Presets.Should().Contain(
        [
            PixelArtPreset.Slime, PixelArtPreset.Cat, PixelArtPreset.Fox, PixelArtPreset.Owl,
            PixelArtPreset.Frog, PixelArtPreset.Ghost, PixelArtPreset.Robot, PixelArtPreset.Duck,
            PixelArtPreset.Penguin, PixelArtPreset.Bunny, PixelArtPreset.Corgi, PixelArtPreset.Dragon,
            PixelArtPreset.Astronaut, PixelArtPreset.Mushroom, PixelArtPreset.Crystal, PixelArtPreset.Campfire,
            PixelArtPreset.Coffee, PixelArtPreset.Sakura
        ]);

        vm.SelectedPreset.Should().Be(PixelArtPreset.Slime);
        vm.NextPresetCommand.Execute(null);
        vm.SelectedPreset.Should().Be(PixelArtPreset.Cat);
        vm.PreviousPresetCommand.Execute(null);
        vm.SelectedPreset.Should().Be(PixelArtPreset.Slime);

        vm.GetCurrentFrameColors().Should().HaveCount(25);
    }

    [Fact]
    public void PixelArt_AdvancesFrames_AndObeysPauseOnHideGpuLaw()
    {
        using var vm = new PixelArtWidgetViewModel(startActive: true);
        vm.IsTimerRunning.Should().BeTrue();

        vm.AdvanceFrame();
        vm.CurrentFrameIndex.Should().Be(1);
        vm.FrameCounter.Should().Be(1);

        vm.AdvanceFrame();
        vm.AdvanceFrame();
        vm.AdvanceFrame();
        vm.CurrentFrameIndex.Should().Be(0); // Loops mod 4
        vm.FrameCounter.Should().Be(4);

        // Pause-on-Hide GPU Law
        vm.OnLauncherHidden();
        vm.IsTimerRunning.Should().BeFalse();

        // Resume on Show
        vm.OnLauncherShown();
        vm.IsTimerRunning.Should().BeTrue();

        // Dispose stops timer
        vm.Dispose();
        vm.IsTimerRunning.Should().BeFalse();
    }

    [Theory]
    [InlineData(WidgetPresetSize.Small, 160, 160)]
    [InlineData(WidgetPresetSize.Medium, 240, 240)]
    [InlineData(WidgetPresetSize.Wide, 320, 160)]
    [InlineData(WidgetPresetSize.Large, 320, 320)]
    public void Photo_SupportsFourPresetSizes_HexFill_AndTransparentToggle(
        WidgetPresetSize size,
        double expectedWidth,
        double expectedHeight)
    {
        using var vm = new PhotoWidgetViewModel();

        vm.Family.Should().Be(WidgetFamily.Photo);
        vm.AvailableSizes.Should().HaveCount(4);

        vm.SetPresetSizeCommand.Execute(size);
        vm.WidgetWidth.Should().Be(expectedWidth);
        vm.WidgetHeight.Should().Be(expectedHeight);

        vm.FillColorHex = "#FF5733";
        vm.ResolvedBackgroundColor.R.Should().Be(255);
        vm.ResolvedBackgroundColor.G.Should().Be(87);
        vm.ResolvedBackgroundColor.B.Should().Be(51);
        vm.ResolvedBackgroundColor.A.Should().Be(255);

        vm.ToggleTransparentBackgroundCommand.Execute(null);
        vm.IsTransparentBackground.Should().BeTrue();
        vm.ResolvedBackgroundColor.A.Should().Be(0);

        vm.HasImage.Should().BeFalse();
        vm.PlaceholderVisibility.Should().Be(Visibility.Visible);
        vm.ImagePath = @"C:\wallpapers\cover.png";
        vm.HasImage.Should().BeTrue();
        vm.ImageVisibility.Should().Be(Visibility.Visible);
        vm.PlaceholderVisibility.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void ShapePhoto_SupportsSixShapeMasks_AndFourSizes()
    {
        using var vm = new ShapePhotoWidgetViewModel();

        vm.Family.Should().Be(WidgetFamily.ShapePhoto);
        vm.AvailableShapes.Should().HaveCount(6);
        vm.AvailableShapes.Should().Contain(
        [
            ShapeMaskType.Circle,
            ShapeMaskType.Squircle,
            ShapeMaskType.Hexagon,
            ShapeMaskType.Diamond,
            ShapeMaskType.Star,
            ShapeMaskType.Heart
        ]);

        vm.SelectShapeCommand.Execute(ShapeMaskType.Heart);
        vm.SelectedShape.Should().Be(ShapeMaskType.Heart);

        vm.NextShapeCommand.Execute(null);
        vm.SelectedShape.Should().Be(ShapeMaskType.Circle);

        vm.SetPresetSizeCommand.Execute(WidgetPresetSize.Wide);
        vm.WidgetWidth.Should().Be(320);
        vm.WidgetHeight.Should().Be(160);
    }

    [Fact]
    public void Gif_ObeysPauseOnHideGpuLaw_AndTransparentBackground()
    {
        using var vm = new GifWidgetViewModel();
        var playbackEvents = new List<bool>();
        vm.PlaybackStateChanged += (_, playing) => playbackEvents.Add(playing);

        vm.Family.Should().Be(WidgetFamily.Gif);
        vm.IsPlaying.Should().BeFalse();

        // Loading a GIF starts playback when launcher is visible
        vm.GifPath = @"C:\media\nyan.gif";
        vm.HasGif.Should().BeTrue();
        vm.IsPlaying.Should().BeTrue();
        vm.GifVisibility.Should().Be(Visibility.Visible);

        // Launcher hidden pauses playback immediately
        vm.OnLauncherHidden();
        vm.IsPlaying.Should().BeFalse();

        // Launcher shown resumes playback because GIF is loaded
        vm.OnLauncherShown();
        vm.IsPlaying.Should().BeTrue();

        // Transparent toggle
        vm.ToggleTransparentBackgroundCommand.Execute(null);
        vm.IsTransparentBackground.Should().BeTrue();
        vm.ResolvedBackgroundColor.A.Should().Be(0);

        playbackEvents.Should().ContainInOrder(true, false, true);

        vm.Dispose();
        vm.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void WidgetCatalog_BroadcastsLifecycleAcrossAllElevenWidgets()
    {
        var mediaMock = new Mock<IMediaSessionService>();
        using var catalog = new WidgetCatalogViewModel(mediaMock.Object);

        catalog.Widgets.Should().HaveCount(11);
        catalog.Gif.GifPath = "animated.gif";

        catalog.OnLauncherShown();
        catalog.Clock.IsTimerRunning.Should().BeTrue();
        catalog.PixelArt.IsTimerRunning.Should().BeTrue();
        catalog.Gif.IsPlaying.Should().BeTrue();

        catalog.OnLauncherHidden();
        catalog.Clock.IsTimerRunning.Should().BeFalse();
        catalog.PixelArt.IsTimerRunning.Should().BeFalse();
        catalog.Gif.IsPlaying.Should().BeFalse();

        catalog.Dispose();
        catalog.Widgets.Should().BeEmpty();
    }
}
