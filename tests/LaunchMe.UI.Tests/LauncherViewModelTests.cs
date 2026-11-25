using FluentAssertions;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Contracts.Media;
using LaunchMe.Contracts.Search;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Contracts.Workflows;
using LaunchMe.Core.Apps;
using LaunchMe.Platform.Win32.Shell;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Catalog;
using Microsoft.UI.Xaml;
using Moq;
using Xunit;

namespace LaunchMe.UI.Tests;

public sealed class LauncherViewModelTests
{
    private readonly Mock<IAppIndexer> _appIndexerMock = new();
    private readonly Mock<IFileSearchService> _fileSearchMock = new();
    private readonly Mock<IClipboardMonitor> _clipboardMock = new();
    private readonly Mock<ICalculatorService> _calculatorMock = new();
    private readonly Mock<IEmojiService> _emojiMock = new();
    private readonly Mock<ISpaceStore> _spaceStoreMock = new();
    private readonly Mock<IAppLauncherService> _appLauncherMock = new();
    private readonly Mock<IClipboardWriter> _clipboardWriterMock = new();
    private readonly Mock<IMediaSessionService> _mediaServiceMock = new();
    private readonly Mock<IWorkflowRunner> _workflowRunnerMock = new();
    private readonly Mock<ISpaceSchedulerService> _spaceSchedulerMock = new();
    private readonly Mock<IThemeAssociationService> _themeAssociationMock = new();

    private LauncherViewModel CreateViewModel(
        WidgetCatalogViewModel? widgetCatalog = null,
        PlatformDiagnosticsService? diagnosticsService = null)
    {
        return new LauncherViewModel(
            _appIndexerMock.Object,
            _fileSearchMock.Object,
            _clipboardMock.Object,
            _calculatorMock.Object,
            _emojiMock.Object,
            _spaceStoreMock.Object,
            _appLauncherMock.Object,
            _clipboardWriterMock.Object,
            widgetCatalog,
            _workflowRunnerMock.Object,
            _spaceSchedulerMock.Object,
            _themeAssociationMock.Object,
            diagnosticsService);
    }

    [Fact]
    public void ClassicView_PopulatesPages_CorrectlyPartitioned()
    {
        // Arrange: 30 apps should create 2 pages (24 items per page)
        var apps = new List<AppItem>();
        for (int i = 0; i < 30; i++)
        {
            apps.Add(new AppItem
            {
                Id = $"app_{i}",
                Name = $"App {i}",
                ExecutablePath = $"C:\\Apps\\{i}.exe",
                IconCachePath = $"icon_{i}.png"
            });
        }
        _appIndexerMock.Setup(x => x.SearchApps(string.Empty)).Returns(apps);

        // Act
        var vm = new LauncherViewModel(
            _appIndexerMock.Object,
            _fileSearchMock.Object,
            _clipboardMock.Object,
            _calculatorMock.Object,
            _emojiMock.Object,
            _spaceStoreMock.Object);

        // Assert
        vm.Pages.Should().HaveCount(2);
        vm.Pages[0].Items.Should().HaveCount(24);
        vm.Pages[1].Items.Should().HaveCount(6);
        vm.PageIndicators.Should().HaveCount(2);
    }

    [Fact]
    public void PauseOnHide_GPU_Law_FiresHiddenEvent_AndClearsTransientState()
    {
        // Arrange
        var vm = CreateViewModel();
        bool eventFired = false;
        vm.LauncherHidden += (_, _) => eventFired = true;
        vm.SearchQuery = "test search";

        // Act
        vm.OnHidden();

        // Assert
        eventFired.Should().BeTrue();
        vm.SearchQuery.Should().BeEmpty();
        vm.SearchResults.Should().BeEmpty();
        vm.IsEmojiMode.Should().BeFalse();
    }

    [Fact]
    public async Task TabKey_SwitchesToEmojiMode_AndQueriesEmojiService()
    {
        // Arrange
        _emojiMock.Setup(x => x.SearchEmojis("rocket"))
            .Returns([new EmojiItem { Character = "🚀", Name = "rocket", Category = "Travel & Places" }]);

        var vm = CreateViewModel();

        // Act
        vm.ToggleEmojiMode();
        vm.IsEmojiMode.Should().BeTrue();

        vm.SearchQuery = "rocket";
        await Task.Delay(100);

        // Assert
        vm.SearchResults.Should().ContainSingle();
        vm.SearchResults[0].Title.Should().Be("🚀");
        vm.SearchResults[0].CategoryName.Should().Be("Emoji");
    }

    [Fact]
    public async Task UnifiedSearch_CombinesCalculator_Apps_Files_AndClipboard()
    {
        // Arrange
        _calculatorMock.Setup(x => x.Evaluate("6*7")).Returns(new CalculationResult
        {
            Expression = "6*7",
            FormattedResult = "42",
            Result = 42,
            IsValid = true
        });
        _appIndexerMock.Setup(x => x.SearchApps("6*7")).Returns(new List<AppItem>
        {
            new() { Id = "calc", Name = "Calculator", ExecutablePath = "calc.exe" }
        });
        _fileSearchMock.Setup(x => x.SearchFilesAsync("6*7", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileSearchResult>());
        _clipboardMock.Setup(x => x.GetItems(ClipboardCategory.Recent)).Returns(new List<ClipboardEntry>());

        var vm = CreateViewModel();

        // Act
        vm.SearchQuery = "6*7";
        await Task.Delay(100);

        // Assert
        vm.SearchResults.Should().HaveCount(2);
        vm.SearchResults[0].CategoryName.Should().Be("Calculator");
        vm.SearchResults[0].Title.Should().Be("42");
        vm.SearchResults[1].CategoryName.Should().Be("Application");
    }

    [Fact]
    public void BatchSelection_CreatesSmartFolder_AndRemovesOriginalItems()
    {
        // Arrange
        var apps = new List<AppItem>
        {
            new() { Id = "1", Name = "App 1", ExecutablePath = "1.exe" },
            new() { Id = "2", Name = "App 2", ExecutablePath = "2.exe" },
            new() { Id = "3", Name = "App 3", ExecutablePath = "3.exe" }
        };
        _appIndexerMock.Setup(x => x.SearchApps(string.Empty)).Returns(apps);
        var vm = CreateViewModel();

        var page = vm.Pages[0];
        vm.SelectedBatchItems.Add(page.Items[0]);
        vm.SelectedBatchItems.Add(page.Items[1]);

        // Act
        vm.BatchCreateFolderFromSelection("Productivity");

        // Assert
        page.Items.Should().HaveCount(2); // App 3 + New Folder
        page.Items.Should().Contain(x => x.Type == LauncherItemType.Folder && x.DisplayName == "Productivity");
        var folderItem = page.Items[1];
        folderItem.FolderData.Should().NotBeNull();
        folderItem.FolderData!.Children.Should().HaveCount(2);
    }

    [Fact]
    public void ExecuteItem_OnApp_CallsAppLauncher_AndRaisesDismissRequested()
    {
        // Arrange
        var targetApp = new AppItem { Id = "notepad", Name = "Notepad", ExecutablePath = "C:\\Windows\\notepad.exe" };
        _appIndexerMock.Setup(x => x.AllApps).Returns([targetApp]);
        _appIndexerMock.Setup(x => x.SearchApps(string.Empty)).Returns([targetApp]);
        _appLauncherMock.Setup(x => x.LaunchAppAsync(targetApp, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var vm = CreateViewModel();
        bool dismissed = false;
        vm.DismissRequested += (_, _) => dismissed = true;

        // Act
        vm.ExecuteItem(vm.Pages[0].Items[0]);

        // Assert
        _appLauncherMock.Verify(x => x.LaunchAppAsync(targetApp, It.IsAny<CancellationToken>()), Times.Once);
        dismissed.Should().BeTrue();
    }

    [Fact]
    public void ExecuteItem_OnFolder_OpensAndClosesFolderModal()
    {
        // Arrange
        var folder = new LauncherFolderViewModel("Dev Tools", FolderGridDensity.Grid4x6);
        var folderItem = new LauncherItemViewModel(folder.Id, folder.Name, "\uE8B7", LauncherItemType.Folder, folder);
        var vm = CreateViewModel();

        // Act: Open
        vm.ExecuteItem(folderItem);

        // Assert: Opened
        vm.IsFolderOpen.Should().BeTrue();
        vm.FolderModalVisibility.Should().Be(Visibility.Visible);
        vm.ActiveFolder.Should().BeSameAs(folder);

        // Act: Close
        vm.CloseFolderCommand.Execute(null);

        // Assert: Closed
        vm.IsFolderOpen.Should().BeFalse();
        vm.FolderModalVisibility.Should().Be(Visibility.Collapsed);
        vm.ActiveFolder.Should().BeNull();
    }

    [Fact]
    public async Task SearchResultActions_CopyToClipboard_LaunchApp_AndOpenFile()
    {
        // Arrange
        var matchedApp = new AppItem { Id = "term", Name = "Terminal", ExecutablePath = "wt.exe" };
        _calculatorMock.Setup(x => x.Evaluate("7*8")).Returns(new CalculationResult
        {
            Expression = "7*8",
            FormattedResult = "56",
            Result = 56,
            IsValid = true
        });
        _appIndexerMock.Setup(x => x.SearchApps("7*8")).Returns([matchedApp]);
        _fileSearchMock.Setup(x => x.SearchFilesAsync("7*8", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new FileSearchResult { FileName = "7*8_notes.txt", Path = @"C:\docs\notes.txt" }]);
        _clipboardMock.Setup(x => x.GetItems(ClipboardCategory.Recent))
            .Returns([new ClipboardEntry { TextContent = "7*8=56", CapturedAtUtc = DateTime.UtcNow }]);

        _emojiMock.Setup(x => x.SearchEmojis("fire"))
            .Returns([new EmojiItem { Character = "🔥", Name = "Fire", Category = "Travel" }]);

        var vm = CreateViewModel();

        // Act: Unified search
        await vm.ExecuteSearchAsync("7*8");
        vm.SearchResults.Should().HaveCount(4);

        // 1. Calculator copies formatted result
        vm.ExecuteSearchResult(vm.SearchResults[0]);
        _clipboardWriterMock.Verify(x => x.SetText("56"), Times.Once);

        // 2. Application launches app
        vm.ExecuteSearchResult(vm.SearchResults[1]);
        _appLauncherMock.Verify(x => x.LaunchAppAsync(matchedApp, It.IsAny<CancellationToken>()), Times.Once);

        // 3. File opens file
        vm.ExecuteSearchResult(vm.SearchResults[2]);
        _fileSearchMock.Verify(x => x.OpenFile(@"C:\docs\notes.txt"), Times.Once);

        // 4. Clipboard copies text
        vm.ExecuteSearchResult(vm.SearchResults[3]);
        _clipboardWriterMock.Verify(x => x.SetText("7*8=56"), Times.Once);

        // 5. Emoji copies glyph
        vm.ToggleEmojiMode();
        await vm.ExecuteSearchAsync("fire");
        vm.SearchResults.Should().ContainSingle();
        vm.ExecuteSearchResult(vm.SearchResults[0]);
        _clipboardWriterMock.Verify(x => x.SetText("🔥"), Times.Once);
    }

    [Fact]
    public void OnShownAndOnHidden_ForwardLifecycleToWidgetCatalog()
    {
        // Arrange
        using var catalog = new WidgetCatalogViewModel(_mediaServiceMock.Object);
        var vm = CreateViewModel(catalog);
        vm.WidgetCatalog.Should().BeSameAs(catalog);

        // Act & Assert: Hide
        vm.OnHidden();
        catalog.Clock.IsTimerRunning.Should().BeFalse();

        // Act & Assert: Show
        vm.OnShown();
        catalog.Clock.IsTimerRunning.Should().BeTrue();
    }

    [Fact]
    public void Commands_ToggleDrawerSettings_AndRaiseQuitRequested()
    {
        // Arrange
        var vm = CreateViewModel();
        bool quitRequested = false;
        vm.QuitAppRequested += (_, _) => quitRequested = true;

        // Act & Assert
        vm.IsWidgetDrawerOpen.Should().BeFalse();
        vm.ToggleWidgetDrawerCommand.Execute(null);
        vm.IsWidgetDrawerOpen.Should().BeTrue();
        vm.WidgetDrawerVisibility.Should().Be(Visibility.Visible);

        vm.IsSettingsOpen.Should().BeFalse();
        vm.ToggleSettingsCommand.Execute(null);
        vm.IsSettingsOpen.Should().BeTrue();
        vm.SettingsOverlayVisibility.Should().Be(Visibility.Visible);
        vm.DiagnosticsSummary.Should().Contain("Space:");

        vm.QuitAppCommand.Execute(null);
        quitRequested.Should().BeTrue();
    }

    [Fact]
    public async Task SmartFolderCreation_PersistsLayoutToActiveSpace()
    {
        // Arrange
        var activeSpace = new SpaceModel { SpaceId = "space-1", Name = "Work" };
        _spaceStoreMock.Setup(x => x.GetActiveSpaceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeSpace);
        _spaceStoreMock.Setup(x => x.SaveSpaceAsync(activeSpace, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var apps = new List<AppItem>
        {
            new() { Id = "a1", Name = "App 1", ExecutablePath = "1.exe" },
            new() { Id = "a2", Name = "App 2", ExecutablePath = "2.exe" }
        };
        _appIndexerMock.Setup(x => x.SearchApps(string.Empty)).Returns(apps);

        var vm = CreateViewModel();
        vm.SelectedBatchItems.Add(vm.Pages[0].Items[0]);
        vm.SelectedBatchItems.Add(vm.Pages[0].Items[1]);

        // Act
        vm.BatchCreateFolderFromSelection("Tools");
        await Task.Delay(100);

        // Assert
        _spaceStoreMock.Verify(x => x.SaveSpaceAsync(
            It.Is<SpaceModel>(s => s.SpaceId == "space-1" && s.Launcher.Folders.Count == 1 && s.Launcher.Folders[0].Title == "Tools"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispose_DisposesWallpaper_WidgetCatalog_AndSearchCts_Idempotently()
    {
        // Arrange
        _fileSearchMock
            .Setup(x => x.SearchFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(50, ct);
                return Array.Empty<FileSearchResult>();
            });
        _clipboardMock.Setup(x => x.GetItems(ClipboardCategory.Recent)).Returns([]);
        _appIndexerMock.Setup(x => x.SearchApps(It.IsAny<string>())).Returns([]);

        var catalog = new WidgetCatalogViewModel(_mediaServiceMock.Object);
        var vm = CreateViewModel(catalog);
        vm.ApplyWallpaper(new WallpaperSettings { Type = WallpaperType.LiveVideo, Asset = "loop.mp4" });
        vm.Wallpaper.IsPlaying.Should().BeTrue();
        catalog.Clock.IsTimerRunning.Should().BeTrue();

        // Trigger overlapping searches to exercise previous CTS disposal
        _ = vm.ExecuteSearchAsync("first");
        await vm.ExecuteSearchAsync("second");

        // Act: Dispose twice to verify idempotency
        vm.Dispose();
        vm.Dispose();

        // Assert
        vm.Wallpaper.IsPlaying.Should().BeFalse();
        vm.Wallpaper.ActiveGpuEstimatePercent.Should().Be(0.0);
        catalog.Clock.IsTimerRunning.Should().BeFalse();
    }

    [Fact]
    public async Task SpacesAndThemes_SwitchExportAndImport_UpdatesActiveSpaceAndWorkflows()
    {
        // Arrange
        var workSpace = new SpaceModel
        {
            SpaceId = "space-work",
            Name = "Work Focus",
            Theme = new ThemeSettings { TintHex = "#047857" },
            Workflows =
            [
                new WorkflowDefinition
                {
                    Id = "wf-1",
                    Name = "Morning Dev",
                    Actions = [new WorkflowAction { ActionType = WorkflowActionType.Delay, DelayMs = 1 }]
                }
            ]
        };
        var personalSpace = new SpaceModel { SpaceId = "space-home", Name = "Personal" };

        _spaceStoreMock.Setup(x => x.GetAllSpacesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([personalSpace, workSpace]);
        _spaceStoreMock.Setup(x => x.SwitchSpaceAsync("space-work", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _spaceStoreMock.Setup(x => x.GetActiveSpaceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(workSpace);
        _spaceStoreMock.Setup(x => x.ExportThemeAsync("space-work", @"C:\tmp\work.launchmetheme", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _spaceStoreMock.Setup(x => x.ImportThemeAsync(@"C:\tmp\imported.launchmetheme", It.IsAny<CancellationToken>()))
            .ReturnsAsync(personalSpace);
        _spaceStoreMock.Setup(x => x.SwitchSpaceAsync("space-home", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var vm = CreateViewModel();

        // Act
        await vm.RefreshSpacesAsync();
        await vm.SwitchSpaceAsync("space-work");

        // Assert switch
        vm.Spaces.Should().HaveCount(2);
        vm.ActiveSpaceName.Should().Be("Work Focus");
        vm.SelectedSpaceName.Should().Be("Work Focus");
        vm.Workflows.Should().ContainSingle(w => w.Name == "Morning Dev");
        vm.WorkflowsStripVisibility.Should().Be(Visibility.Visible);

        // Act export & import
        await vm.ExportActiveThemeAsync(@"C:\tmp\work.launchmetheme");
        _spaceStoreMock.Verify(x => x.ExportThemeAsync("space-work", @"C:\tmp\work.launchmetheme", It.IsAny<CancellationToken>()), Times.Once);

        var imported = await vm.ImportThemePackageAsync(@"C:\tmp\imported.launchmetheme");
        imported.SpaceId.Should().Be("space-home");
        _spaceStoreMock.Verify(x => x.SwitchSpaceAsync("space-home", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Workflows_ExecuteAndMatchInUnifiedSearch()
    {
        // Arrange
        var wf = new WorkflowDefinition
        {
            Id = "wf-focus",
            Name = "Deep Focus Setup",
            Actions = [new WorkflowAction { ActionType = WorkflowActionType.OpenUrl, Target = "https://example.com" }]
        };
        var space = new SpaceModel { SpaceId = "s1", Name = "Focus", Workflows = [wf] };

        _fileSearchMock.Setup(x => x.SearchFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _clipboardMock.Setup(x => x.GetItems(ClipboardCategory.Recent)).Returns([]);
        _appIndexerMock.Setup(x => x.SearchApps(It.IsAny<string>())).Returns([]);

        using var vm = CreateViewModel();
        vm.ApplySpace(space);

        // Act: Command execution
        vm.ExecuteWorkflowCommand.Execute(wf);
        await Task.Delay(50);
        _workflowRunnerMock.Verify(x => x.ExecuteWorkflowAsync(wf, It.IsAny<CancellationToken>()), Times.Once);

        // Act: Unified search match
        await vm.ExecuteSearchAsync("Deep Focus");
        vm.SearchResults.Should().ContainSingle(r => r.CategoryName == "Workflow" && r.Title == "Deep Focus Setup");
    }

    [Fact]
    public void Categories_FilterMockedIndexerApps_Cleanly()
    {
        // Arrange
        var apps = new List<AppItem>
        {
            new() { Id = "vscode", Name = "VS Code", Categories = ["Development"], IsRecentlyUsed = true },
            new() { Id = "edge", Name = "Microsoft Edge", Categories = ["Browsers"] },
            new() { Id = "secret", Name = "Internal Tool", Categories = ["Development"], IsHidden = true }
        };
        _appIndexerMock.Setup(x => x.SearchApps(string.Empty)).Returns(apps);

        using var vm = CreateViewModel();

        // Default "All" excludes hidden
        vm.Categories.Should().Contain(["All", "Recently Used", "Development", "Browsers", "Media", "Productivity", "Hidden"]);
        vm.Pages[0].Items.Select(i => i.Id).Should().BeEquivalentTo(["vscode", "edge"]);

        // "Development" excludes hidden
        vm.SelectCategoryCommand.Execute("Development");
        vm.SelectedCategory.Should().Be("Development");
        vm.Pages[0].Items.Select(i => i.Id).Should().BeEquivalentTo(["vscode"]);

        // "Recently Used"
        vm.SelectCategoryCommand.Execute("Recently Used");
        vm.Pages[0].Items.Select(i => i.Id).Should().BeEquivalentTo(["vscode"]);

        // "Hidden"
        vm.SelectCategoryCommand.Execute("Hidden");
        vm.Pages[0].Items.Select(i => i.Id).Should().BeEquivalentTo(["secret"]);
    }

    [Fact]
    public async Task AppManagement_CleanUninstallHideAndRename_InvokeIndexerAndRefreshGrid()
    {
        // Arrange
        var apps = new List<AppItem>
        {
            new() { Id = "app1", Name = "Sample App", ExecutablePath = "sample.exe" }
        };
        _appIndexerMock.Setup(x => x.SearchApps(string.Empty)).Returns(apps);
        _appIndexerMock.Setup(x => x.CleanUninstallAsync("app1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                apps.Clear();
                return true;
            });

        using var vm = CreateViewModel();

        // Act & Assert: Hide & Rename
        vm.HideApp("app1", true);
        _appIndexerMock.Verify(x => x.HideApp("app1", true), Times.Once);

        vm.RenameApp("app1", "Renamed App");
        _appIndexerMock.Verify(x => x.RenameApp("app1", "Renamed App"), Times.Once);

        // Act & Assert: Clean Uninstall
        bool removed = await vm.CleanUninstallAppAsync("app1");
        removed.Should().BeTrue();
        _appIndexerMock.Verify(x => x.CleanUninstallAsync("app1", true, It.IsAny<CancellationToken>()), Times.Once);
        vm.Pages[0].Items.Should().BeEmpty();
    }

    [Fact]
    public void SchedulerAndThemeAssociationToggles_AndDiagnostics_WorkCorrectly()
    {
        // Arrange
        bool schedulerRunning = false;
        _spaceSchedulerMock.SetupGet(x => x.IsRunning).Returns(() => schedulerRunning);
        _spaceSchedulerMock.Setup(x => x.Start()).Callback(() => schedulerRunning = true);
        _spaceSchedulerMock.Setup(x => x.Stop()).Callback(() => schedulerRunning = false);

        bool associationRegistered = false;
        _themeAssociationMock.SetupGet(x => x.IsRegistered).Returns(() => associationRegistered);
        _themeAssociationMock.Setup(x => x.RegisterFileAssociation(It.IsAny<string>()))
            .Callback(() => associationRegistered = true);
        _themeAssociationMock.Setup(x => x.UnregisterFileAssociation())
            .Callback(() => associationRegistered = false);

        using var vm = CreateViewModel(diagnosticsService: new PlatformDiagnosticsService());

        // Assert diagnostics populated
        vm.DiagnosticsSnapshot.Should().NotBeNull();
        vm.PlatformDiagnosticsText.Should().Contain("DPI:");

        // Toggle scheduler
        vm.IsSchedulerEnabled.Should().BeFalse();
        vm.ToggleSchedulerCommand.Execute(null);
        vm.IsSchedulerEnabled.Should().BeTrue();
        vm.ToggleSchedulerCommand.Execute(null);
        vm.IsSchedulerEnabled.Should().BeFalse();

        // Toggle theme association
        vm.IsThemeAssociationRegistered.Should().BeFalse();
        vm.ToggleThemeAssociationCommand.Execute(null);
        vm.IsThemeAssociationRegistered.Should().BeTrue();
        vm.ToggleThemeAssociationCommand.Execute(null);
        vm.IsThemeAssociationRegistered.Should().BeFalse();
    }
}
