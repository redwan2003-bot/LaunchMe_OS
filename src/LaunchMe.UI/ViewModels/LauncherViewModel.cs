using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Contracts.Launcher;
using LaunchMe.Contracts.Search;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Core.Spaces;
using LaunchMe.UI.Theming;
using LaunchMe.UI.Wallpaper;
using LaunchMe.UI.Widgets.Catalog;
using Microsoft.UI.Xaml;

namespace LaunchMe.UI.ViewModels;

public sealed class LauncherViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IAppIndexer _appIndexer;
    private readonly IFileSearchService _fileSearchService;
    private readonly IClipboardMonitor _clipboardMonitor;
    private readonly ICalculatorService _calculatorService;
    private readonly IEmojiService _emojiService;
    private readonly ISpaceStore _spaceStore;
    private readonly IAppLauncherService? _appLauncher;
    private readonly IClipboardWriter? _clipboardWriter;
    private readonly LiquidGlassTintService _tintService = new();

    private string _searchQuery = string.Empty;
    private bool _isEmojiMode;
    private bool _isEditMode;
    private bool _isWidgetDrawerOpen;
    private bool _isSettingsOpen;
    private bool _isFolderOpen;
    private LauncherFolderViewModel? _activeFolder;
    private string _activeSpaceName = "Default Space";
    private string _currentTintHex = "#1E40AF";
    private int _currentPageIndex;
    private int _selectedSearchIndex = -1;
    private LauncherItemViewModel? _draggedItem;
    private CancellationTokenSource? _searchCts;
    private bool _disposed;

    public event EventHandler? DismissRequested;
    public event EventHandler? LauncherHidden;
    public event EventHandler? LauncherShown;
    public event EventHandler? QuitAppRequested;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ThemeVisualState ThemeVisualState { get; } = new();
    public LiveWallpaperViewModel Wallpaper { get; } = new();
    public WidgetCatalogViewModel? WidgetCatalog { get; }
    public ObservableCollection<LauncherPageViewModel> Pages { get; } = new();
    public ObservableCollection<PageIndicatorViewModel> PageIndicators { get; } = new();
    public ObservableCollection<UnifiedSearchResultViewModel> SearchResults { get; } = new();
    public ObservableCollection<LauncherItemViewModel> SelectedBatchItems { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                OnPropertyChanged();
                _ = ExecuteSearchAsync(value);
            }
        }
    }

    public bool IsEmojiMode
    {
        get => _isEmojiMode;
        private set
        {
            if (_isEmojiMode != value)
            {
                _isEmojiMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmojiModeVisible));
            }
        }
    }

    public bool IsEmojiModeVisible => _isEmojiMode;
    public bool HasSearchResults => SearchResults.Count > 0;

    public int SelectedSearchIndex
    {
        get => _selectedSearchIndex;
        set
        {
            if (_selectedSearchIndex != value)
            {
                _selectedSearchIndex = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode != value)
            {
                _isEditMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditModeButtonText));
                foreach (var page in Pages)
                {
                    foreach (var item in page.Items)
                    {
                        item.IsEditModeActive = _isEditMode;
                    }
                }
            }
        }
    }

    public string EditModeButtonText => _isEditMode ? "Done" : "Edit Layout";

    public bool IsWidgetDrawerOpen
    {
        get => _isWidgetDrawerOpen;
        set
        {
            if (_isWidgetDrawerOpen != value)
            {
                _isWidgetDrawerOpen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WidgetDrawerVisibility));
            }
        }
    }

    public Visibility WidgetDrawerVisibility => _isWidgetDrawerOpen ? Visibility.Visible : Visibility.Collapsed;

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set
        {
            if (_isSettingsOpen != value)
            {
                _isSettingsOpen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SettingsOverlayVisibility));
                if (_isSettingsOpen)
                {
                    OnPropertyChanged(nameof(IndexedAppsCount));
                    OnPropertyChanged(nameof(DiagnosticsSummary));
                }
            }
        }
    }

    public Visibility SettingsOverlayVisibility => _isSettingsOpen ? Visibility.Visible : Visibility.Collapsed;

    public bool IsFolderOpen
    {
        get => _isFolderOpen;
        set
        {
            if (_isFolderOpen != value)
            {
                _isFolderOpen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FolderModalVisibility));
            }
        }
    }

    public Visibility FolderModalVisibility => _isFolderOpen ? Visibility.Visible : Visibility.Collapsed;

    public LauncherFolderViewModel? ActiveFolder
    {
        get => _activeFolder;
        private set
        {
            if (_activeFolder != value)
            {
                _activeFolder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveFolderName));
                OnPropertyChanged(nameof(ActiveFolderChildren));
            }
        }
    }

    public string ActiveFolderName => _activeFolder?.Name ?? "Folder";
    public ObservableCollection<LauncherItemViewModel>? ActiveFolderChildren => _activeFolder?.Children;

    public string ActiveSpaceName
    {
        get => _activeSpaceName;
        private set
        {
            if (_activeSpaceName != value)
            {
                _activeSpaceName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DiagnosticsSummary));
            }
        }
    }

    public int IndexedAppsCount =>
        (_appIndexer.AllApps?.Count is > 0 ? _appIndexer.AllApps.Count : _appIndexer.SearchApps(string.Empty)?.Count) ?? 0;

    public string DiagnosticsSummary =>
        $"Space: {ActiveSpaceName}  |  Apps: {IndexedAppsCount}  |  Pages: {Pages.Count}  |  Tint: {_currentTintHex}";

    public int CurrentPageIndex
    {
        get => _currentPageIndex;
        set
        {
            if (_currentPageIndex != value && value >= 0 && value < Pages.Count)
            {
                _currentPageIndex = value;
                OnPropertyChanged();
                UpdatePageIndicators();
            }
        }
    }

    public ICommand ToggleEditModeCommand { get; }
    public ICommand ToggleWidgetDrawerCommand { get; }
    public ICommand ToggleSettingsCommand { get; }
    public ICommand CloseFolderCommand { get; }
    public ICommand QuitAppCommand { get; }

    public LauncherViewModel(
        IAppIndexer appIndexer,
        IFileSearchService fileSearchService,
        IClipboardMonitor clipboardMonitor,
        ICalculatorService calculatorService,
        IEmojiService emojiService,
        ISpaceStore spaceStore,
        IAppLauncherService? appLauncher = null,
        IClipboardWriter? clipboardWriter = null,
        WidgetCatalogViewModel? widgetCatalog = null)
    {
        _appIndexer = appIndexer ?? throw new ArgumentNullException(nameof(appIndexer));
        _fileSearchService = fileSearchService ?? throw new ArgumentNullException(nameof(fileSearchService));
        _clipboardMonitor = clipboardMonitor ?? throw new ArgumentNullException(nameof(clipboardMonitor));
        _calculatorService = calculatorService ?? throw new ArgumentNullException(nameof(calculatorService));
        _emojiService = emojiService ?? throw new ArgumentNullException(nameof(emojiService));
        _spaceStore = spaceStore ?? throw new ArgumentNullException(nameof(spaceStore));
        _appLauncher = appLauncher;
        _clipboardWriter = clipboardWriter;
        WidgetCatalog = widgetCatalog;

        ToggleEditModeCommand = new RelayCommand(() => IsEditMode = !IsEditMode);
        ToggleWidgetDrawerCommand = new RelayCommand(() => IsWidgetDrawerOpen = !IsWidgetDrawerOpen);
        ToggleSettingsCommand = new RelayCommand(() => IsSettingsOpen = !IsSettingsOpen);
        CloseFolderCommand = new RelayCommand(CloseFolder);
        QuitAppCommand = new RelayCommand(() => QuitAppRequested?.Invoke(this, EventArgs.Empty));

        Wallpaper.AttachToLauncher(this);
        LoadClassicView();
    }

    public void ApplySpace(SpaceModel space)
    {
        ArgumentNullException.ThrowIfNull(space);

        ActiveSpaceName = string.IsNullOrWhiteSpace(space.Name) ? "Default Space" : space.Name;
        ApplyTheme(space.Theme);
        ApplyWallpaper(space.Wallpaper);
    }

    public void ApplyTheme(ThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _currentTintHex = settings.TintHex;
        _tintService.UpdateVisualState(ThemeVisualState, settings);
        OnPropertyChanged(nameof(DiagnosticsSummary));
    }

    public void ApplyQuickThemeTint(string tintHex)
    {
        if (string.IsNullOrWhiteSpace(tintHex)) return;

        var settings = new ThemeSettings
        {
            Style = ThemeVisualState.IsLiquidGlass ? "LiquidGlass" : "Flat",
            Mode = ThemeVisualState.IsTinted ? "Tinted" : "Clear",
            TintHex = tintHex,
            Intensity = ThemeVisualState.TintOpacity,
            TitleColor = "Auto"
        };

        ApplyTheme(settings);
        _ = PersistThemeToActiveSpaceAsync(settings);
    }

    public void ApplyWallpaper(WallpaperSettings settings)
    {
        Wallpaper.ApplySettings(settings);
    }

    public void OnShown()
    {
        LauncherShown?.Invoke(this, EventArgs.Empty);
        Wallpaper.OnLauncherShown();
        WidgetCatalog?.OnLauncherShown();
    }

    public void OnHidden()
    {
        // Pause-on-Hide GPU Law: Immediately stop timers, render loops, and video decoding
        LauncherHidden?.Invoke(this, EventArgs.Empty);
        Wallpaper.OnLauncherHidden();
        WidgetCatalog?.OnLauncherHidden();
        SearchQuery = string.Empty;
        SearchResults.Clear();
        IsEmojiMode = false;
        CloseFolder();
        IsSettingsOpen = false;
    }

    public void CloseFolder()
    {
        IsFolderOpen = false;
        ActiveFolder = null;
    }

    public void LoadClassicView()
    {
        Pages.Clear();
        var allApps = _appIndexer.SearchApps(string.Empty) ?? [];
        const int itemsPerPage = 24; // 4 rows x 6 columns
        int totalPages = Math.Max(1, (int)Math.Ceiling(allApps.Count / (double)itemsPerPage));

        for (int p = 0; p < totalPages; p++)
        {
            var page = new LauncherPageViewModel(p);
            var pageApps = allApps.Skip(p * itemsPerPage).Take(itemsPerPage);
            foreach (var app in pageApps)
            {
                page.Items.Add(new LauncherItemViewModel(app.Id, app.DisplayName, app.IconCachePath ?? string.Empty, LauncherItemType.App));
            }
            Pages.Add(page);
        }

        if (_currentPageIndex >= Pages.Count)
        {
            _currentPageIndex = 0;
            OnPropertyChanged(nameof(CurrentPageIndex));
        }

        UpdatePageIndicators();
        OnPropertyChanged(nameof(IndexedAppsCount));
        OnPropertyChanged(nameof(DiagnosticsSummary));
    }

    public void ToggleEmojiMode()
    {
        IsEmojiMode = !IsEmojiMode;
        _ = ExecuteSearchAsync(SearchQuery);
    }

    public async Task ExecuteSearchAsync(string query)
    {
        if (_disposed) return;

        var previousCts = _searchCts;
        previousCts?.Cancel();
        previousCts?.Dispose();

        var currentCts = new CancellationTokenSource();
        _searchCts = currentCts;
        var token = currentCts.Token;

        SearchResults.Clear();
        if (string.IsNullOrWhiteSpace(query))
        {
            OnPropertyChanged(nameof(HasSearchResults));
            return;
        }

        if (IsEmojiMode)
        {
            var emojis = _emojiService.SearchEmojis(query);
            foreach (var emoji in emojis)
            {
                var capturedEmoji = emoji;
                SearchResults.Add(new UnifiedSearchResultViewModel(
                    capturedEmoji.Character,
                    capturedEmoji.Name,
                    "Emoji",
                    "\uE76E",
                    () =>
                    {
                        _clipboardWriter?.SetText(capturedEmoji.Character);
                        DismissRequested?.Invoke(this, EventArgs.Empty);
                    }));
            }
        }
        else
        {
            // 1. Calculator Check
            var calcResult = _calculatorService.Evaluate(query);
            if (calcResult != null && calcResult.IsValid)
            {
                var formatted = calcResult.FormattedResult;
                SearchResults.Add(new UnifiedSearchResultViewModel(
                    formatted,
                    $"Math Expression = {query}",
                    "Calculator",
                    "\uE1D0",
                    () =>
                    {
                        _clipboardWriter?.SetText(formatted);
                        DismissRequested?.Invoke(this, EventArgs.Empty);
                    }));
            }

            // 2. Apps Search
            var apps = _appIndexer.SearchApps(query);
            foreach (var app in apps.Take(5))
            {
                var capturedApp = app;
                SearchResults.Add(new UnifiedSearchResultViewModel(
                    capturedApp.DisplayName,
                    capturedApp.ExecutablePath,
                    "Application",
                    "\uE71D",
                    () =>
                    {
                        _ = _appLauncher?.LaunchAppAsync(capturedApp);
                        DismissRequested?.Invoke(this, EventArgs.Empty);
                    }));
            }

            // 3. Files Search
            IReadOnlyList<FileSearchResult> files;
            try
            {
                files = await _fileSearchService.SearchFilesAsync(query, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_disposed || token.IsCancellationRequested) return;

            foreach (var file in files.Take(5))
            {
                var capturedFile = file;
                SearchResults.Add(new UnifiedSearchResultViewModel(
                    capturedFile.FileName,
                    capturedFile.Path,
                    "File",
                    "\uE8B7",
                    () =>
                    {
                        _fileSearchService.OpenFile(capturedFile.Path);
                        DismissRequested?.Invoke(this, EventArgs.Empty);
                    }));
            }

            // 4. Clipboard History
            var clips = _clipboardMonitor.GetItems(ClipboardCategory.Recent);
            var matchingClips = clips
                .Where(c => !string.IsNullOrEmpty(c.TextContent) && c.TextContent.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(3);
            foreach (var clip in matchingClips)
            {
                var capturedClipText = clip.TextContent ?? string.Empty;
                SearchResults.Add(new UnifiedSearchResultViewModel(
                    capturedClipText,
                    $"Copied {clip.CapturedAtUtc:HH:mm:ss}",
                    "Clipboard",
                    "\uE77F",
                    () =>
                    {
                        _clipboardWriter?.SetText(capturedClipText);
                        DismissRequested?.Invoke(this, EventArgs.Empty);
                    }));
            }
        }

        SelectedSearchIndex = SearchResults.Count > 0 ? 0 : -1;
        OnPropertyChanged(nameof(HasSearchResults));
    }

    public void NavigateSearchSelection(int offset)
    {
        if (SearchResults.Count == 0) return;
        int next = SelectedSearchIndex + offset;
        if (next < 0) next = 0;
        if (next >= SearchResults.Count) next = SearchResults.Count - 1;
        SelectedSearchIndex = next;
    }

    public void ExecuteSelectedSearchResult()
    {
        if (SelectedSearchIndex >= 0 && SelectedSearchIndex < SearchResults.Count)
        {
            SearchResults[SelectedSearchIndex].Action?.Invoke();
        }
    }

    public void ExecuteSearchResult(UnifiedSearchResultViewModel result)
    {
        result.Action?.Invoke();
    }

    public void ExecuteItem(LauncherItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        switch (item.Type)
        {
            case LauncherItemType.App:
            {
                var app = ResolveIndexedApp(item.Id);
                if (app != null)
                {
                    _ = _appLauncher?.LaunchAppAsync(app);
                }

                CloseFolder();
                DismissRequested?.Invoke(this, EventArgs.Empty);
                break;
            }
            case LauncherItemType.Folder:
            {
                if (item.FolderData != null)
                {
                    ActiveFolder = item.FolderData;
                    IsFolderOpen = true;
                }
                break;
            }
            case LauncherItemType.EmbeddedFolder:
            {
                string? folderPath = item.FolderData?.AttachedFolderPath;
                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    OpenFolderInExplorer(folderPath);
                }
                DismissRequested?.Invoke(this, EventArgs.Empty);
                break;
            }
        }
    }

    public void StartDrag(LauncherItemViewModel item) => _draggedItem = item;

    public void HandleDropOnItem(LauncherItemViewModel target)
    {
        if (_draggedItem == null || _draggedItem == target) return;

        // Smart Folder creation: Dropping one item on another creates a folder
        var currentPage = Pages[CurrentPageIndex];
        var folder = new LauncherFolderViewModel("New Folder", FolderGridDensity.Grid4x6);
        folder.AddItem(_draggedItem);
        folder.AddItem(target);

        int targetIndex = currentPage.Items.IndexOf(target);
        if (targetIndex >= 0)
        {
            currentPage.Items.Remove(_draggedItem);
            currentPage.Items.Remove(target);
            currentPage.Items.Insert(targetIndex, new LauncherItemViewModel(folder.Id, folder.Name, "\uE8B7", LauncherItemType.Folder, folder));
        }

        _draggedItem = null;
        _ = PersistLayoutToActiveSpaceAsync();
    }

    public void BatchCreateFolderFromSelection(string folderName)
    {
        if (SelectedBatchItems.Count < 2) return;

        var currentPage = Pages[CurrentPageIndex];
        var folder = new LauncherFolderViewModel(folderName, FolderGridDensity.Grid4x6);
        foreach (var item in SelectedBatchItems.ToList())
        {
            folder.AddItem(item);
            currentPage.Items.Remove(item);
        }

        currentPage.Items.Add(new LauncherItemViewModel(folder.Id, folder.Name, "\uE8B7", LauncherItemType.Folder, folder));
        SelectedBatchItems.Clear();
        _ = PersistLayoutToActiveSpaceAsync();
    }

    private AppItem? ResolveIndexedApp(string appId)
    {
        var allApps = _appIndexer.AllApps;
        if (allApps == null || allApps.Count == 0)
        {
            allApps = _appIndexer.SearchApps(string.Empty);
        }

        return allApps?.FirstOrDefault(a => string.Equals(a.Id, appId, StringComparison.OrdinalIgnoreCase));
    }

    private void OpenFolderInExplorer(string folderPath)
    {
        if (_appLauncher != null && _appLauncher.LaunchTarget(folderPath))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folderPath}\"")
            {
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore shell launch errors for missing paths
        }
    }

    private async Task PersistLayoutToActiveSpaceAsync()
    {
        try
        {
            var activeSpace = await _spaceStore.GetActiveSpaceAsync().ConfigureAwait(false);
            if (activeSpace == null)
            {
                return;
            }

            var contractPages = new List<LauncherPage>(Pages.Count);
            var contractFolders = new List<LauncherFolder>();

            foreach (var pageVm in Pages)
            {
                var page = new LauncherPage
                {
                    PageIndex = pageVm.PageNumber
                };

                for (int i = 0; i < pageVm.Items.Count; i++)
                {
                    var itemVm = pageVm.Items[i];
                    page.Items.Add(MapItemToContract(itemVm, i));

                    if (itemVm.FolderData != null)
                    {
                        contractFolders.Add(MapFolderToContract(itemVm.FolderData, itemVm.Type == LauncherItemType.EmbeddedFolder));
                    }
                }

                contractPages.Add(page);
            }

            await SpaceLayoutSerializer.SaveLayoutAsync(_spaceStore, activeSpace, contractPages, contractFolders)
                .ConfigureAwait(false);
        }
        catch
        {
            // Non-fatal persistence failure should not disrupt launcher UI interaction
        }
    }

    private async Task PersistThemeToActiveSpaceAsync(ThemeSettings theme)
    {
        try
        {
            var activeSpace = await _spaceStore.GetActiveSpaceAsync().ConfigureAwait(false);
            if (activeSpace == null) return;

            activeSpace.Theme = theme;
            await _spaceStore.SaveSpaceAsync(activeSpace).ConfigureAwait(false);
        }
        catch
        {
            // Ignore transient storage errors
        }
    }

    private static LauncherItem MapItemToContract(LauncherItemViewModel itemVm, int index)
    {
        var contractType = itemVm.Type switch
        {
            LauncherItemType.App => LaunchMe.Contracts.Launcher.LauncherItemType.App,
            LauncherItemType.Folder => LaunchMe.Contracts.Launcher.LauncherItemType.Folder,
            LauncherItemType.EmbeddedFolder => LaunchMe.Contracts.Launcher.LauncherItemType.EmbeddedFolder,
            LauncherItemType.Widget => LaunchMe.Contracts.Launcher.LauncherItemType.Widget,
            LauncherItemType.Workflow => LaunchMe.Contracts.Launcher.LauncherItemType.Workflow,
            _ => LaunchMe.Contracts.Launcher.LauncherItemType.App
        };

        return new LauncherItem
        {
            Id = itemVm.Id,
            Title = itemVm.DisplayName,
            ItemType = contractType,
            GridRow = index / 6,
            GridColumn = index % 6,
            AppId = itemVm.Type == LauncherItemType.App ? itemVm.Id : null,
            FolderId = itemVm.FolderData?.Id
        };
    }

    private static LauncherFolder MapFolderToContract(LauncherFolderViewModel folderVm, bool isEmbedded)
    {
        var folder = new LauncherFolder
        {
            Id = folderVm.Id,
            Title = folderVm.Name,
            ColorHex = folderVm.CustomHexColor,
            Emoji = folderVm.CustomEmoji,
            GridRows = folderVm.Density.Rows,
            GridColumns = folderVm.Density.Columns,
            IsEmbeddedFolder = isEmbedded,
            FolderPath = folderVm.AttachedFolderPath
        };

        for (int i = 0; i < folderVm.Children.Count; i++)
        {
            folder.Items.Add(MapItemToContract(folderVm.Children[i], i));
        }

        return folder;
    }

    private void UpdatePageIndicators()
    {
        PageIndicators.Clear();
        for (int i = 0; i < Pages.Count; i++)
        {
            PageIndicators.Add(new PageIndicatorViewModel(i, i == CurrentPageIndex ? "#38BDF8" : "#4B5563"));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_searchCts != null)
        {
            _searchCts.Cancel();
            _searchCts.Dispose();
            _searchCts = null;
        }

        Wallpaper.Dispose();
        WidgetCatalog?.Dispose();
    }
}
