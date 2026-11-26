using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace LaunchMe.UI.ViewModels;

public enum LauncherItemType { App, Folder, EmbeddedFolder, Widget, Workflow }

public sealed record FolderGridDensity(int Rows, int Columns)
{
    public static FolderGridDensity Grid3x5 => new(3, 5);
    public static FolderGridDensity Grid4x6 => new(4, 6);
    public static FolderGridDensity Grid5x8 => new(5, 8);
}

public sealed class LauncherPageViewModel : INotifyPropertyChanged
{
    private int _pageNumber;

    public int PageNumber
    {
        get => _pageNumber;
        set
        {
            if (_pageNumber != value)
            {
                _pageNumber = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<LauncherItemViewModel> Items { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public LauncherPageViewModel(int pageNumber)
    {
        _pageNumber = pageNumber;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class LauncherItemViewModel : INotifyPropertyChanged
{
    private string _displayName;
    private bool _isTitleVisible = true;
    private bool _isEditModeActive;
    private string _dominantGlowHex;

    public string Id { get; }
    public string IconImageSource { get; }
    public LauncherItemType Type { get; }
    public LauncherFolderViewModel? FolderData { get; }

    public string DominantGlowHex
    {
        get => _dominantGlowHex;
        set { if (_dominantGlowHex != value) { _dominantGlowHex = value; OnPropertyChanged(); } }
    }

    public string DisplayName
    {
        get => _displayName;
        set { if (_displayName != value) { _displayName = value; OnPropertyChanged(); } }
    }

    public bool IsTitleVisible
    {
        get => _isTitleVisible;
        set { if (_isTitleVisible != value) { _isTitleVisible = value; OnPropertyChanged(); } }
    }

    public bool IsEditModeActive
    {
        get => _isEditModeActive;
        set { if (_isEditModeActive != value) { _isEditModeActive = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public LauncherItemViewModel(string id, string displayName, string iconSource, LauncherItemType type, LauncherFolderViewModel? folderData = null)
    {
        Id = id;
        _displayName = displayName;
        IconImageSource = iconSource;
        Type = type;
        FolderData = folderData;
        _dominantGlowHex = ComputeDominantGlowHex(displayName, type);
    }

    public static string ComputeDominantGlowHex(string name, LauncherItemType type)
    {
        if (type == LauncherItemType.Folder) return "#2CA855F7";
        int hash = Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(name ?? string.Empty));
        return (hash % 6) switch
        {
            0 => "#2800F0FF", // Electric Cyan
            1 => "#28A855F7", // Ultra Orchid
            2 => "#2800FF9D", // Phosphor Emerald
            3 => "#28F59E0B", // Amber Topaz
            4 => "#2838BDF8", // Sky Sapphire
            _ => "#28F43F5E"  // Ruby Rose
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class LauncherFolderViewModel : INotifyPropertyChanged
{
    private string _name;
    private FolderGridDensity _density;
    private string? _attachedFolderPath;
    private string? _customHexColor;
    private string? _customEmoji;

    public string Id { get; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    public FolderGridDensity Density
    {
        get => _density;
        set { if (_density != value) { _density = value; OnPropertyChanged(); } }
    }

    public string? AttachedFolderPath
    {
        get => _attachedFolderPath;
        set { if (_attachedFolderPath != value) { _attachedFolderPath = value; OnPropertyChanged(); } }
    }

    public string? CustomHexColor
    {
        get => _customHexColor;
        set { if (_customHexColor != value) { _customHexColor = value; OnPropertyChanged(); } }
    }

    public string? CustomEmoji
    {
        get => _customEmoji;
        set { if (_customEmoji != value) { _customEmoji = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<LauncherItemViewModel> Children { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public LauncherFolderViewModel(string name, FolderGridDensity density)
    {
        _name = name;
        _density = density;
    }

    public void AddItem(LauncherItemViewModel item)
    {
        if (!Children.Contains(item))
        {
            Children.Add(item);
        }
    }

    public void SortAlphabetically()
    {
        var sorted = Children.OrderBy(x => x.DisplayName).ToList();
        Children.Clear();
        foreach (var item in sorted) Children.Add(item);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class UnifiedSearchResultViewModel
{
    public string Title { get; }
    public string Subtitle { get; }
    public string CategoryName { get; }
    public string CategoryGlyph { get; }
    public Action? Action { get; }

    public UnifiedSearchResultViewModel(string title, string subtitle, string categoryName, string categoryGlyph, Action? action)
    {
        Title = title;
        Subtitle = subtitle;
        CategoryName = categoryName;
        CategoryGlyph = categoryGlyph;
        Action = action;
    }
}

public sealed class PageIndicatorViewModel : INotifyPropertyChanged
{
    private string _dotColor;
    public int PageIndex { get; }

    public string DotColor
    {
        get => _dotColor;
        set { if (_dotColor != value) { _dotColor = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PageIndicatorViewModel(int pageIndex, string dotColor)
    {
        PageIndex = pageIndex;
        _dotColor = dotColor;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(ConvertParameter(parameter)) ?? true;
    public void Execute(object? parameter) => _execute(ConvertParameter(parameter));
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private static T? ConvertParameter(object? parameter)
    {
        if (parameter is T typed) return typed;
        if (parameter is null) return default;
        return default;
    }
}
