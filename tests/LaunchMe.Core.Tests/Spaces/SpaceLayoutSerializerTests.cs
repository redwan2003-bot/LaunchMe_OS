using FluentAssertions;
using LaunchMe.Contracts.Launcher;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Core.Spaces;
using Xunit;

namespace LaunchMe.Core.Tests.Spaces;

public sealed class SpaceLayoutSerializerTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteSpaceStore _store;

    public SpaceLayoutSerializerTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"launchme_layout_test_{Guid.NewGuid():N}.db");
        _store = new SqliteSpaceStore(_testDbPath);
    }

    [Fact]
    public void SerializeAndDeserializeLayout_RoundtripsPagesAndFolders()
    {
        var settings = new LauncherSettings
        {
            ClassicView = false,
            FreeLayout = true,
            HideTitles = true,
            IconScale = 1.25,
            FolderGrid = "5x6",
            Pages =
            [
                new LauncherPage
                {
                    PageIndex = 1,
                    Items =
                    [
                        new LauncherItem
                        {
                            Id = "item-2",
                            Title = "Notes",
                            ItemType = LauncherItemType.Folder,
                            GridRow = 0,
                            GridColumn = 2,
                            FolderId = "folder-1"
                        }
                    ]
                },
                new LauncherPage
                {
                    PageIndex = 0,
                    Items =
                    [
                        new LauncherItem
                        {
                            Id = "item-1",
                            Title = "Terminal",
                            ItemType = LauncherItemType.App,
                            GridRow = 1,
                            GridColumn = 3,
                            AppId = "uwp:Microsoft.WindowsTerminal"
                        }
                    ]
                }
            ],
            Folders =
            [
                new LauncherFolder
                {
                    Id = "folder-1",
                    Title = "Dev Tools",
                    ColorHex = "#1E40AF",
                    Emoji = "🛠️",
                    GridColumns = 4,
                    GridRows = 6,
                    Items =
                    [
                        new LauncherItem
                        {
                            Id = "sub-1",
                            Title = "Git",
                            ItemType = LauncherItemType.App,
                            AppId = "lnk:git"
                        }
                    ]
                }
            ]
        };

        var json = SpaceLayoutSerializer.SerializeLayout(settings);
        var restored = SpaceLayoutSerializer.DeserializeLayout(json);

        restored.ClassicView.Should().BeFalse();
        restored.FreeLayout.Should().BeTrue();
        restored.HideTitles.Should().BeTrue();
        restored.IconScale.Should().Be(1.25);
        restored.FolderGrid.Should().Be("5x6");
        restored.Pages.Should().HaveCount(2);
        restored.Pages[0].PageIndex.Should().Be(0);
        restored.Pages[0].Items[0].AppId.Should().Be("uwp:Microsoft.WindowsTerminal");
        restored.Pages[1].PageIndex.Should().Be(1);
        restored.Folders.Should().HaveCount(1);
        restored.Folders[0].Title.Should().Be("Dev Tools");
        restored.Folders[0].Items.Should().HaveCount(1);
    }

    [Fact]
    public void ApplyAndExtractLayout_NormalizesOrderAndDecouplesCollections()
    {
        var space = new SpaceModel { SpaceId = "custom", Name = "Custom" };
        var pages = new List<LauncherPage>
        {
            new() { PageIndex = 2 },
            new() { PageIndex = 0 }
        };
        var folders = new List<LauncherFolder>
        {
            new() { Id = "f1", Title = "Work" }
        };

        SpaceLayoutSerializer.ApplyLayout(space, pages, folders);
        pages.Clear();
        folders.Clear();

        var snapshot = SpaceLayoutSerializer.ExtractLayout(space);
        snapshot.Pages.Select(p => p.PageIndex).Should().Equal(0, 2);
        snapshot.Folders.Should().ContainSingle(f => f.Id == "f1" && f.Title == "Work");
    }

    [Fact]
    public async Task SaveActiveLayoutAsync_PersistsPagesAndFoldersToSqliteStore()
    {
        var pages = new[]
        {
            new LauncherPage
            {
                PageIndex = 0,
                Items =
                [
                    new LauncherItem
                    {
                        Id = "app-item-1",
                        Title = "Browser",
                        ItemType = LauncherItemType.App,
                        GridRow = 0,
                        GridColumn = 0,
                        AppId = "lnk:edge"
                    }
                ]
            }
        };

        var folders = new[]
        {
            new LauncherFolder
            {
                Id = "folder-media",
                Title = "Media",
                Emoji = "🎵"
            }
        };

        await SpaceLayoutSerializer.SaveActiveLayoutAsync(_store, pages, folders);

        var reloaded = await _store.GetActiveSpaceAsync();
        reloaded.Should().NotBeNull();
        reloaded!.Launcher.Pages.Should().ContainSingle();
        reloaded.Launcher.Pages[0].Items.Should().ContainSingle(i => i.AppId == "lnk:edge");
        reloaded.Launcher.Folders.Should().ContainSingle(f => f.Id == "folder-media" && f.Title == "Media");
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
