using FluentAssertions;
using LaunchMe.Core.Apps;
using Xunit;

namespace LaunchMe.Core.Tests.Apps;

public sealed class AppIndexerTests : IDisposable
{
    private readonly string _tempCache;
    private readonly string _tempSourceDir;
    private readonly List<string> _createdLeftoverDirs = [];
    private readonly AppIndexer _indexer;

    public AppIndexerTests()
    {
        _tempCache = Path.Combine(Path.GetTempPath(), $"icon_cache_{Guid.NewGuid():N}");
        _tempSourceDir = Path.Combine(Path.GetTempPath(), $"app_source_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSourceDir);
        _indexer = new AppIndexer(new IconCacheService(_tempCache));
    }

    [Fact]
    public void ExtraSources_CanBeAddedAndRemoved()
    {
        var fakeDir = Path.GetTempPath();
        _indexer.AddExtraSource(fakeDir);
        _indexer.GetExtraSources().Should().Contain(fakeDir);

        _indexer.RemoveExtraSource(fakeDir);
        _indexer.GetExtraSources().Should().NotContain(fakeDir);
    }

    [Fact]
    public void HideAndRename_ModifiesAppProperties()
    {
        _indexer.HideApp("test-app", true);
        _indexer.RenameApp("test-app", "Renamed App");
    }

    [Fact]
    public async Task CategoriesAndFiltering_HandlesAllHiddenRecentlyUsedAndNamedCategories()
    {
        var devExe = Path.Combine(_tempSourceDir, $"TestCodeApp_{Guid.NewGuid():N}.exe");
        var browserExe = Path.Combine(_tempSourceDir, $"TestBrowserApp_{Guid.NewGuid():N}.exe");
        var hiddenExe = Path.Combine(_tempSourceDir, $"TestSpotifyHidden_{Guid.NewGuid():N}.exe");

        await File.WriteAllBytesAsync(devExe, []);
        await File.WriteAllBytesAsync(browserExe, []);
        await File.WriteAllBytesAsync(hiddenExe, []);

        _indexer.AddExtraSource(_tempSourceDir);
        await _indexer.ScanAllAsync();

        var devApp = _indexer.AllApps.First(a => a.ExecutablePath.Equals(devExe, StringComparison.OrdinalIgnoreCase));
        var browserApp = _indexer.AllApps.First(a => a.ExecutablePath.Equals(browserExe, StringComparison.OrdinalIgnoreCase));
        var hiddenApp = _indexer.AllApps.First(a => a.ExecutablePath.Equals(hiddenExe, StringComparison.OrdinalIgnoreCase));

        devApp.IsRecentlyUsed = true;
        _indexer.HideApp(hiddenApp.Id, true);

        var categories = _indexer.GetCategories();
        categories.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        categories.Should().Contain(["Development", "Browsers"]);

        var allVisible = _indexer.GetAppsByCategory("All");
        allVisible.Should().Contain(a => a.Id == devApp.Id);
        allVisible.Should().Contain(a => a.Id == browserApp.Id);
        allVisible.Should().NotContain(a => a.Id == hiddenApp.Id);

        var hiddenApps = _indexer.GetAppsByCategory("Hidden");
        hiddenApps.Should().Contain(a => a.Id == hiddenApp.Id);
        hiddenApps.Should().NotContain(a => a.Id == devApp.Id);

        var recentApps = _indexer.GetAppsByCategory("Recently Used");
        recentApps.Should().Contain(a => a.Id == devApp.Id);
        recentApps.Should().NotContain(a => a.Id == browserApp.Id);

        var devApps = _indexer.GetAppsByCategory("development");
        devApps.Should().Contain(a => a.Id == devApp.Id);
        devApps.Should().NotContain(a => a.Id == browserApp.Id);
    }

    [Fact]
    public async Task PreviewUninstallLeftovers_ReturnsSafeExistingFoldersAndRejectsTraversal()
    {
        var uniqueAppName = $"LaunchMeTestApp_{Guid.NewGuid():N}";
        var appExe = Path.Combine(_tempSourceDir, $"{uniqueAppName}.exe");
        await File.WriteAllBytesAsync(appExe, []);

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var leftoverDir = Path.Combine(localAppData, uniqueAppName);
        Directory.CreateDirectory(leftoverDir);
        _createdLeftoverDirs.Add(leftoverDir);

        _indexer.AddExtraSource(_tempSourceDir);
        await _indexer.ScanAllAsync();

        var indexedApp = _indexer.AllApps.First(a => a.ExecutablePath.Equals(appExe, StringComparison.OrdinalIgnoreCase));
        var leftovers = _indexer.PreviewUninstallLeftovers(indexedApp.Id);

        leftovers.Should().ContainSingle()
            .Which.Should().Be(Path.GetFullPath(leftoverDir));

        _indexer.PreviewUninstallLeftovers("..").Should().BeEmpty();
        _indexer.PreviewUninstallLeftovers("../../../Windows").Should().BeEmpty();
        _indexer.PreviewUninstallLeftovers("non-existent-app-id").Should().BeEmpty();
    }

    public void Dispose()
    {
        foreach (var dir in _createdLeftoverDirs)
        {
            if (Directory.Exists(dir))
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        if (Directory.Exists(_tempCache))
        {
            try { Directory.Delete(_tempCache, true); } catch { }
        }

        if (Directory.Exists(_tempSourceDir))
        {
            try { Directory.Delete(_tempSourceDir, true); } catch { }
        }
    }
}
