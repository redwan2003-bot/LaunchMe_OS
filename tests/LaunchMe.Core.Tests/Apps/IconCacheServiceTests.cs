using System.Drawing;
using System.Drawing.Imaging;
using FluentAssertions;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Core.Apps;
using Moq;
using Xunit;

namespace LaunchMe.Core.Tests.Apps;

public sealed class IconCacheServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _cacheDir;

    public IconCacheServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"launchme_icon_test_{Guid.NewGuid():N}");
        _cacheDir = Path.Combine(_tempRoot, "cache");
        Directory.CreateDirectory(_cacheDir);
    }

    [Fact]
    public void ParameterlessConstructor_RemainsBackwardsCompatible()
    {
        var service = new IconCacheService();
        service.Should().NotBeNull();
    }

    [Fact]
    public void GetOrExtractIcon_ReturnsCachedFileFirst_WithoutCallingShellExtractor()
    {
        var mockExtractor = new Mock<IShellIconExtractor>(MockBehavior.Strict);
        var service = new IconCacheService(_cacheDir, mockExtractor.Object);

        var cachedPath = Path.Combine(_cacheDir, "cached-app_128.png");
        File.WriteAllBytes(cachedPath, CreateSolidPngBytes(128, 128, Color.CornflowerBlue));

        var result = service.GetOrExtractIcon("cached-app", @"C:\nonexistent\app.exe", 128);

        result.Should().Be(cachedPath);
        mockExtractor.Verify(
            x => x.ExtractHighResPng(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void GetOrExtractIcon_UsesMockShellIconExtractor_ForHighRes256Png()
    {
        var appDir = Path.Combine(_tempRoot, "win32app");
        Directory.CreateDirectory(appDir);
        var exePath = Path.Combine(appDir, "sample.exe");
        File.WriteAllBytes(exePath, [0x4D, 0x5A]);

        var highResPng = CreateSolidPngBytes(256, 256, Color.FromArgb(255, 24, 144, 255));
        var mockExtractor = new Mock<IShellIconExtractor>();
        mockExtractor
            .Setup(x => x.ExtractHighResPng(exePath, 256, true))
            .Returns(highResPng);

        var service = new IconCacheService(_cacheDir, mockExtractor.Object);
        var iconPath = service.GetOrExtractIcon("win32-highres", exePath, 128);

        File.Exists(iconPath).Should().BeTrue();
        mockExtractor.Verify(x => x.ExtractHighResPng(exePath, 256, true), Times.Once);

        using var bmp = new Bitmap(iconPath);
        bmp.Width.Should().Be(128);
        bmp.Height.Should().Be(128);
        // High-res (>= 96px) is full-bleed resized rather than mounted inside a squircle pedestal
        bmp.GetPixel(2, 2).A.Should().Be(255);
    }

    [Fact]
    public void GetOrExtractIcon_MountsSub96PxIconInsideLuxurySquirclePedestal()
    {
        var appDir = Path.Combine(_tempRoot, "legacyapp");
        Directory.CreateDirectory(appDir);
        var exePath = Path.Combine(appDir, "legacy.exe");
        File.WriteAllBytes(exePath, [0x4D, 0x5A]);

        // Legacy 32x32 crimson icon returned by extractor
        var legacyPng = CreateSolidPngBytes(32, 32, Color.FromArgb(255, 210, 45, 65));
        var mockExtractor = new Mock<IShellIconExtractor>();
        mockExtractor
            .Setup(x => x.ExtractHighResPng(exePath, 256, true))
            .Returns(legacyPng);

        var service = new IconCacheService(_cacheDir, mockExtractor.Object);
        var iconPath = service.GetOrExtractIcon("legacy-32", exePath, 128);

        File.Exists(iconPath).Should().BeTrue();
        using var output = new Bitmap(iconPath);
        output.Width.Should().Be(128);
        output.Height.Should().Be(128);

        // Outer corners outside the G2 squircle remain transparent
        output.GetPixel(0, 0).A.Should().Be(0);
        output.GetPixel(127, 127).A.Should().Be(0);

        // Pedestal ring (inside squircle, outside centered 58% icon) is tinted & opaque
        var pedestalSample = output.GetPixel(16, 64);
        pedestalSample.A.Should().BeGreaterThan(180);
        pedestalSample.R.Should().BeGreaterThan(pedestalSample.B);
    }

    [Fact]
    public void NormalizeToLuxurySquircle_ProducesUniformG2SquircleWithDominantEdgeTint()
    {
        using var source = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(source))
        {
            g.Clear(Color.FromArgb(255, 20, 180, 120));
        }

        using var normalized = IconCacheService.NormalizeToLuxurySquircle(source, 128);

        normalized.Width.Should().Be(128);
        normalized.Height.Should().Be(128);
        normalized.PixelFormat.Should().Be(PixelFormat.Format32bppArgb);
        normalized.GetPixel(1, 1).A.Should().Be(0);

        var pedestalPixel = normalized.GetPixel(16, 64);
        pedestalPixel.A.Should().BeGreaterThan(200);
        pedestalPixel.G.Should().BeGreaterThan(pedestalPixel.R);
    }

    [Fact]
    public void TryFindMsixHighResAsset_PrioritizesScale400OverScale200_AndSkipsExtractor()
    {
        var msixRoot = Path.Combine(_tempRoot, "msix_pkg");
        var assetsDir = Path.Combine(msixRoot, "Assets");
        Directory.CreateDirectory(assetsDir);

        var exePath = Path.Combine(msixRoot, "ModernApp.exe");
        File.WriteAllBytes(exePath, [0x4D, 0x5A]);

        var scale200 = Path.Combine(assetsDir, "Square150x150Logo.scale-200.png");
        var scale400 = Path.Combine(assetsDir, "Square150x150Logo.scale-400.png");
        File.WriteAllBytes(scale200, CreateSolidPngBytes(150, 150, Color.DarkGray));
        File.WriteAllBytes(scale400, CreateSolidPngBytes(300, 300, Color.RoyalBlue));

        var manifestXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
                     xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10">
              <Applications>
                <Application Id="App">
                  <uap:VisualElements Square150x150Logo="Assets\Square150x150Logo.png" />
                </Application>
              </Applications>
            </Package>
            """;
        File.WriteAllText(Path.Combine(msixRoot, "AppxManifest.xml"), manifestXml);

        var mockExtractor = new Mock<IShellIconExtractor>(MockBehavior.Strict);
        var service = new IconCacheService(_cacheDir, mockExtractor.Object);

        var foundAsset = service.TryFindMsixHighResAsset(exePath);
        foundAsset.Should().Be(scale400);

        var cachedIcon = service.GetOrExtractIcon("msix-app", exePath, 128);
        File.Exists(cachedIcon).Should().BeTrue();
        mockExtractor.Verify(
            x => x.ExtractHighResPng(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void TryFindMsixHighResAsset_FallsBackToSiblingAssetsScale200_WhenManifestAbsent()
    {
        var pkgRoot = Path.Combine(_tempRoot, "msix_no_manifest");
        var binDir = Path.Combine(pkgRoot, "bin");
        var assetsDir = Path.Combine(pkgRoot, "Assets");
        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(assetsDir);

        var exePath = Path.Combine(binDir, "App.exe");
        File.WriteAllBytes(exePath, [0x4D, 0x5A]);

        File.WriteAllBytes(Path.Combine(assetsDir, "SplashScreen.scale-400.png"), CreateSolidPngBytes(400, 200, Color.Black));
        var scale200 = Path.Combine(assetsDir, "AppList.scale-200.png");
        File.WriteAllBytes(scale200, CreateSolidPngBytes(96, 96, Color.Teal));

        var service = new IconCacheService(_cacheDir);
        service.TryFindMsixHighResAsset(exePath).Should().Be(scale200);
    }

    [Fact]
    public void GetOrExtractIcon_256px_AppliesG2SquircleMaskAndSpecularRim()
    {
        var exePath = Path.Combine(_tempRoot, "GoldStandardApp.exe");
        File.WriteAllBytes(exePath, [0x4D, 0x5A]);

        var mockExtractor = new Mock<IShellIconExtractor>();
        mockExtractor
            .Setup(x => x.ExtractHighResPng(exePath, 256, true))
            .Returns(CreateSolidPngBytes(512, 512, Color.Navy));

        var service = new IconCacheService(_cacheDir, mockExtractor.Object);
        var cachedIcon = service.GetOrExtractIcon("gold-256", exePath, 256);

        File.Exists(cachedIcon).Should().BeTrue();
        using var result = new Bitmap(cachedIcon);
        result.Width.Should().Be(256);
        result.Height.Should().Be(256);
        result.GetPixel(1, 1).A.Should().Be(0);
        result.GetPixel(128, 128).A.Should().Be(255);
    }

    private static byte[] CreateSolidPngBytes(int width, int height, Color color)
    {
        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(color);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); } catch { }
        }
    }
}
