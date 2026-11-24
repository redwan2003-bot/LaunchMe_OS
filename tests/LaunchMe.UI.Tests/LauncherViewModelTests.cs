using FluentAssertions;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Contracts.Search;
using LaunchMe.Contracts.Spaces;
using LaunchMe.UI.ViewModels;
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

    private LauncherViewModel CreateViewModel()
    {
        return new LauncherViewModel(
            _appIndexerMock.Object,
            _fileSearchMock.Object,
            _clipboardMock.Object,
            _calculatorMock.Object,
            _emojiMock.Object,
            _spaceStoreMock.Object);
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
        var vm = CreateViewModel();

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
}
