using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Contracts.Search;
using LaunchMe.Contracts.Spaces;

namespace LaunchMe.UI.ViewModels;

public sealed class LauncherViewModel : INotifyPropertyChanged
{
    private readonly IAppIndexer _appIndexer;
    private readonly IFileSearchService _fileSearchService;
    private readonly IClipboardMonitor _clipboardMonitor;
    private readonly ICalculatorService _calculatorService;
    private readonly IEmojiService _emojiService;
    private readonly ISpaceStore _spaceStore;

    private string _searchQuery = string.Empty;
    private bool _isEmojiMode;
    private bool _isEditMode;
    private int _currentPageIndex;
    private int _selectedSearchIndex = -1;
    private LauncherItemViewModel? _draggedItem;
    private CancellationTokenSource? _searchCts;

    public event EventHandler? DismissRequested;
    public event EventHandler? LauncherHidden;
    public event EventHandler? LauncherShown;
    public event PropertyChangedEventHandler? PropertyChanged;

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

    public LauncherViewModel(
        IAppIndexer appIndexer,
        IFileSearchService fileSearchService,
        IClipboardMonitor clipboardMonitor,
        ICalculatorService calculatorService,
        IEmojiService emojiService,
        ISpaceStore spaceStore)
    {
        _appIndexer = appIndexer ?? throw new ArgumentNullException(nameof(appIndexer));
        _fileSearchService = fileSearchService ?? throw new ArgumentNullException(nameof(fileSearchService));
        _clipboardMonitor = clipboardMonitor ?? throw new ArgumentNullException(nameof(clipboardMonitor));
        _calculatorService = calculatorService ?? throw new ArgumentNullException(nameof(calculatorService));
        _emojiService = emojiService ?? throw new ArgumentNullException(nameof(emojiService));
        _spaceStore = spaceStore ?? throw new ArgumentNullException(nameof(spaceStore));

        ToggleEditModeCommand = new RelayCommand(() => IsEditMode = !IsEditMode);
        LoadClassicView();
    }

    public void OnShown()
    {
        LauncherShown?.Invoke(this, EventArgs.Empty);
    }

    public void OnHidden()
    {
        // Pause-on-Hide GPU Law: Immediately stop timers, render loops, and video decoding
        LauncherHidden?.Invoke(this, EventArgs.Empty);
        SearchQuery = string.Empty;
        SearchResults.Clear();
        IsEmojiMode = false;
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

        UpdatePageIndicators();
    }

    public void ToggleEmojiMode()
    {
        IsEmojiMode = !IsEmojiMode;
        _ = ExecuteSearchAsync(SearchQuery);
    }

    public async Task ExecuteSearchAsync(string query)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

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
                SearchResults.Add(new UnifiedSearchResultViewModel(
                    emoji.Character,
                    emoji.Name,
                    "Emoji",
                    "\uE76E",
                    () => { DismissRequested?.Invoke(this, EventArgs.Empty); }));
            }
        }
        else
        {
            // 1. Calculator Check
            var calcResult = _calculatorService.Evaluate(query);
            if (calcResult != null && calcResult.IsValid)
            {
                SearchResults.Add(new UnifiedSearchResultViewModel(
                    calcResult.FormattedResult,
                    $"Math Expression = {query}",
                    "Calculator",
                    "\uE1D0",
                    () => { DismissRequested?.Invoke(this, EventArgs.Empty); }));
            }

            // 2. Apps Search
            var apps = _appIndexer.SearchApps(query);
            foreach (var app in apps.Take(5))
            {
                SearchResults.Add(new UnifiedSearchResultViewModel(
                    app.DisplayName,
                    app.ExecutablePath,
                    "Application",
                    "\uE71D",
                    () => { DismissRequested?.Invoke(this, EventArgs.Empty); }));
            }

            // 3. Files Search
            var files = await _fileSearchService.SearchFilesAsync(query, token);
            foreach (var file in files.Take(5))
            {
                SearchResults.Add(new UnifiedSearchResultViewModel(
                    file.FileName,
                    file.Path,
                    "File",
                    "\uE8B7",
                    () => { _fileSearchService.RevealInExplorer(file.Path); DismissRequested?.Invoke(this, EventArgs.Empty); }));
            }

            // 4. Clipboard History
            var clips = _clipboardMonitor.GetItems(ClipboardCategory.Recent);
            var matchingClips = clips.Where(c => !string.IsNullOrEmpty(c.TextContent) && c.TextContent.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(3);
            foreach (var clip in matchingClips)
            {
                SearchResults.Add(new UnifiedSearchResultViewModel(
                    clip.TextContent ?? string.Empty,
                    $"Copied {clip.CapturedAtUtc:HH:mm:ss}",
                    "Clipboard",
                    "\uE77F",
                    () => { DismissRequested?.Invoke(this, EventArgs.Empty); }));
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
        if (item.Type == LauncherItemType.App)
        {
            DismissRequested?.Invoke(this, EventArgs.Empty);
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
}
