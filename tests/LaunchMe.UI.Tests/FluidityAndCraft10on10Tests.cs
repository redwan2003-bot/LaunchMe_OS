using FluentAssertions;
using LaunchMe.Contracts.Media;
using LaunchMe.UI.Theming;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Clock;
using Windows.UI;
using Xunit;

namespace LaunchMe.UI.Tests;

public sealed class FluidityAndCraft10on10Tests
{
    [Fact]
    public void LiquidGlassCompositor_ComputesWallpaperRefractionAndColorMatrix()
    {
        using var compositor = new LiquidGlassCompositor();
        compositor.ChromaticFringePx.Should().Be(1.5f);

        var sampled = compositor.ComputeWallpaperRefractionTint(Color.FromArgb(255, 0, 240, 255), 0.25f);
        sampled.B.Should().BeGreaterThan(LiquidGlassCompositor.ObsidianTintColor.B);

        var matrix = LiquidGlassCompositor.BuildObsidianColorMatrix(sampled);
        matrix.M53.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void MusicPlayer_ExtractsAlbumAuraFromArtworkAndFallback()
    {
        using var vm = new MusicPlayerViewModel();
        string fakeArt = Convert.ToBase64String([20, 180, 250, 90, 210, 140, 60, 240, 120, 45, 220, 190, 80, 110, 200, 255]);

        vm.UpdateFromTrack(new MediaTrackInfo { Title = "Track A", Artist = "Artist A", ThumbnailBase64 = fakeArt, IsPlaying = true });
        vm.AuraPrimaryHex.Should().StartWith("#FF");
        vm.AuraSecondaryHex.Should().StartWith("#FF");

        vm.UpdateFromTrack(new MediaTrackInfo { Title = "Track B", Artist = "Artist B", ThumbnailBase64 = null, IsPlaying = true });
        vm.AuraPrimaryHex.Should().StartWith("#FF");
    }

    [Fact]
    public void ClockWidget_SmoothSweepInterpolatesSubSecondAnglesAndObeysPauseOnHide()
    {
        using var vm = new ClockWidgetViewModel(startActive: true);
        vm.EnableSmoothSweep = true;

        vm.TickSmoothSweep(new DateTime(2026, 11, 26, 12, 0, 15, 500));
        vm.SecondHandAngle.Should().BeApproximately(15.5 * 6.0, 0.05);

        vm.OnLauncherHidden();
        double frozen = vm.SecondHandAngle;
        vm.TickSmoothSweep(new DateTime(2026, 11, 26, 12, 0, 30, 0));
        vm.SecondHandAngle.Should().Be(frozen);
    }

    [Fact]
    public void Dock_GaussianWaveScales_PeaksAtCursorAndTapersAcrossNeighbors()
    {
        double[] scales = DockViewModel.ComputeGaussianWaveScales(5, pointerX: 110.0, itemSpacing: 44.0, maxScale: 1.42, sigma: 68.0);
        scales.Should().HaveCount(5);

        // Item 2 center is 2*44 + 22 = 110.0 -> exact cursor peak
        scales[2].Should().BeApproximately(1.42, 0.001);
        scales[1].Should().BeGreaterThan(1.15).And.BeLessThan(scales[2]);
        scales[3].Should().BeApproximately(scales[1], 0.001);
        scales[0].Should().BeGreaterThan(1.0).And.BeLessThan(scales[1]);
    }

    [Fact]
    public void LauncherItem_AssignsTranslucentDominantGlowHex()
    {
        var app = new LauncherItemViewModel("vs-code", "Visual Studio Code", "code.png", LauncherItemType.App);
        var folder = new LauncherItemViewModel("f1", "Dev", "folder.png", LauncherItemType.Folder);

        app.DominantGlowHex.Should().StartWith("#28").And.HaveLength(9);
        folder.DominantGlowHex.Should().Be("#2CA855F7");
    }
}
