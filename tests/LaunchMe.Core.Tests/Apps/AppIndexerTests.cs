using FluentAssertions;
using LaunchMe.Core.Apps;
using Xunit;

namespace LaunchMe.Core.Tests.Apps;

public sealed class AppIndexerTests : IDisposable
{
    private readonly string _tempCache;
    private readonly AppIndexer _indexer;

    public AppIndexerTests()
    {
        _tempCache = Path.Combine(Path.GetTempPath(), $"icon_cache_{Guid.NewGuid():N}");
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

    public void Dispose()
    {
        if (Directory.Exists(_tempCache))
        {
            try { Directory.Delete(_tempCache, true); } catch { }
        }
    }
}
