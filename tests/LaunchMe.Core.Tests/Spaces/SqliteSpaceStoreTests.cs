using FluentAssertions;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Core.Spaces;
using LaunchMe.Core.Themes;
using Xunit;

namespace LaunchMe.Core.Tests.Spaces;

public sealed class SqliteSpaceStoreTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteSpaceStore _store;

    public SqliteSpaceStoreTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"launchme_test_{Guid.NewGuid():N}.db");
        _store = new SqliteSpaceStore(_testDbPath);
    }

    [Fact]
    public async Task Initialize_SeedsDefaultSpace()
    {
        var spaces = await _store.GetAllSpacesAsync();
        spaces.Should().NotBeEmpty();
        spaces.Should().Contain(s => s.Name == "Default Space");

        var active = await _store.GetActiveSpaceAsync();
        active.Should().NotBeNull();
        active!.Name.Should().Be("Default Space");
    }

    [Fact]
    public async Task SaveSpace_And_SwitchSpace_UpdatesDatabaseAndFiresEvent()
    {
        var newSpace = new SpaceModel
        {
            SpaceId = "work-focus",
            Name = "Deep Work",
            IsDefault = false
        };

        await _store.SaveSpaceAsync(newSpace);

        SpaceChangedEventArgs? firedArgs = null;
        _store.SpaceChanged += (_, args) => firedArgs = args;

        await _store.SwitchSpaceAsync("work-focus");

        var active = await _store.GetActiveSpaceAsync();
        active.Should().NotBeNull();
        active!.SpaceId.Should().Be("work-focus");

        firedArgs.Should().NotBeNull();
        firedArgs!.NewSpaceId.Should().Be("work-focus");
    }

    [Fact]
    public async Task ExportAndImportTheme_RoundtripsSuccessfully()
    {
        var exportZip = Path.Combine(Path.GetTempPath(), $"theme_{Guid.NewGuid():N}.launchmetheme");

        try
        {
            await _store.ExportThemeAsync("default-space", exportZip);
            File.Exists(exportZip).Should().BeTrue();

            var imported = await _store.ImportThemeAsync(exportZip);
            imported.Should().NotBeNull();
            imported.Name.Should().Be("Default Space");
            imported.SpaceId.Should().NotBe("default-space"); // gets fresh ID
            imported.Theme.SpecularIntensity.Should().Be(0.85);
            imported.Theme.EnableGrain.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(exportZip)) File.Delete(exportZip);
        }
    }

    [Fact]
    public async Task SaveSpace_And_ThemeEngineService_ClampSpecularIntensityAndPreserveEnableGrain()
    {
        var space = new SpaceModel
        {
            SpaceId = "luxury-space",
            Name = "Editorial Luxury",
            Theme = new ThemeSettings
            {
                SpecularIntensity = 2.4,
                EnableGrain = false
            }
        };

        await _store.SaveSpaceAsync(space);
        await _store.SwitchSpaceAsync("luxury-space");

        var loaded = await _store.GetActiveSpaceAsync();
        loaded.Should().NotBeNull();
        loaded!.Theme.SpecularIntensity.Should().Be(1.0);
        loaded.Theme.EnableGrain.Should().BeFalse();

        var themeEngine = new ThemeEngineService();
        var resolved = themeEngine.ResolveTheme(new ThemeSettings { SpecularIntensity = -0.5 });
        resolved.SpecularIntensity.Should().Be(0.0);
        resolved.EnableGrain.Should().BeTrue();
    }

    public void Dispose()
    {
        _store.Dispose();
        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); } catch { }
        }
    }
}
