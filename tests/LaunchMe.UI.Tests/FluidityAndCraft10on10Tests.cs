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

    [Fact]
    public void MusicPlayer_AuraColorsParseToWinUIColorStructAndNotify()
    {
        using var vm = new MusicPlayerViewModel();
        var props = new List<string>();
        vm.PropertyChanged += (_, e) => { if (e.PropertyName != null) props.Add(e.PropertyName); };

        string fakeArt = Convert.ToBase64String([240, 80, 120, 200, 90, 130, 210, 70, 110, 220, 60, 140, 250, 85, 115, 205]);
        vm.UpdateFromTrack(new MediaTrackInfo { Title = "Neon", Artist = "Pulse", ThumbnailBase64 = fakeArt, IsPlaying = true });

        vm.AuraPrimaryColor.A.Should().Be(255);
        vm.AuraSecondaryColor.A.Should().Be(255);
        props.Should().Contain(nameof(MusicPlayerViewModel.AuraPrimaryColor));
        props.Should().Contain(nameof(MusicPlayerViewModel.AuraSecondaryColor));
    }

    [Fact]
    public void LauncherItem_SamplesPixelRgbFromIconFileOnDisk()
    {
        string tempIcon = Path.Combine(Path.GetTempPath(), $"launchme-icon-{Guid.NewGuid():N}.bin");
        try
        {
            byte[] payload = new byte[128];
            for (int i = 32; i + 2 < payload.Length; i += 3)
            {
                payload[i] = 40;
                payload[i + 1] = 200;
                payload[i + 2] = 120;
            }
            File.WriteAllBytes(tempIcon, payload);

            var item = new LauncherItemViewModel("app-disk", "DiskApp", tempIcon, LauncherItemType.App);
            item.DominantGlowHex.Should().StartWith("#28").And.HaveLength(9);
            item.DominantGlowHex.Should().NotBe("#28000000");
        }
        finally
        {
            if (File.Exists(tempIcon)) File.Delete(tempIcon);
        }
    }

    [Fact]
    public void ShowcaseCaptureService_DetectsCliFlagAccurately()
    {
        LaunchMe.UI.Services.ShowcaseCaptureService.IsShowcaseMode(["--capture-showcase"]).Should().BeTrue();
        LaunchMe.UI.Services.ShowcaseCaptureService.IsShowcaseMode(["--smoke-test"]).Should().BeFalse();
    }

    [Fact]
    public void App_DispatchDockToolBarAction_HandlesAllActionsAndBluetoothWatcherDisposesCleanly()
    {
        using var notch = new NotchViewModel();
        var launched = new List<string>();
        bool locked = false;

        App.DispatchDockToolBarAction("focus", notch).Should().BeTrue();
        notch.FocusModeActive.Should().BeTrue();

        App.DispatchDockToolBarAction("screenshot", notch, t => launched.Add(t)).Should().BeTrue();
        App.DispatchDockToolBarAction("terminal", notch, t => launched.Add(t)).Should().BeTrue();
        App.DispatchDockToolBarAction("lock", notch, lockAction: () => locked = true).Should().BeTrue();
        App.DispatchDockToolBarAction("unknown", notch).Should().BeFalse();

        launched.Should().BeEquivalentTo(["ms-screenclip:", "wt.exe"]);
        locked.Should().BeTrue();

        using var bt = new LaunchMe.UI.Services.BluetoothWatcherService();
        bt.Start();
        bt.Dispose();
        bt.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void GoldStandard_ClipboardImageDragFile_And_FileThumbnailCache_Work()
    {
        // 1. Finder-level Clipboard Image Drag-Out materialization
        string dragDir = Path.Combine(Path.GetTempPath(), $"launchme-drag-test-{Guid.NewGuid():N}");
        try
        {
            var entry = new LaunchMe.Contracts.Clipboard.ClipboardEntry
            {
                Id = "img-drag-1",
                Category = LaunchMe.Contracts.Clipboard.ClipboardCategory.Images,
                DataPayloadBase64 = "data:image/png;base64," + Convert.ToBase64String([137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3, 4])
            };

            string? materialized = LaunchMe.UI.Services.ClipboardDragPayloadHelper.MaterializeImageDragFile(entry, dragDir);
            materialized.Should().NotBeNullOrWhiteSpace();
            File.Exists(materialized!).Should().BeTrue();
            File.ReadAllBytes(materialized!).Should().HaveCount(12);
        }
        finally
        {
            if (Directory.Exists(dragDir)) Directory.Delete(dragDir, recursive: true);
        }

        // 2. QLThumbnail-style file thumbnail caching
        string cacheDir = Path.Combine(Path.GetTempPath(), $"launchme-thumb-test-{Guid.NewGuid():N}");
        string sampleFile = Path.Combine(cacheDir, "doc.pdf");
        try
        {
            Directory.CreateDirectory(cacheDir);
            File.WriteAllText(sampleFile, "pdf-mock");

            var mockExtractor = new Moq.Mock<LaunchMe.Contracts.Indexing.IShellIconExtractor>();
            mockExtractor.Setup(x => x.ExtractHighResPng(sampleFile, 256, true))
                .Returns([137, 80, 78, 71, 13, 10, 26, 10]);

            var iconCache = new LaunchMe.Core.Apps.IconCacheService(cacheDir, mockExtractor.Object);
            string? thumb = iconCache.GetOrExtractFileThumbnail(sampleFile, 256);
            thumb.Should().NotBeNullOrWhiteSpace();
            File.Exists(thumb!).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(cacheDir)) Directory.Delete(cacheDir, recursive: true);
        }
    }
}
