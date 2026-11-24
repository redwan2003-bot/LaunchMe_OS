using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Models;

namespace LaunchMe.UI.Widgets.Affirmations;

public sealed record AffirmationItem(string Text, string Author, string Category = "Inspiration");

public sealed class AffirmationWidgetViewModel : IWidgetViewModel
{
    private static readonly IReadOnlyList<AffirmationItem> DefaultQuotes =
    [
        new("Simplicity is the soul of efficiency.", "Austin Freeman", "Focus"),
        new("Make each day your masterpiece.", "John Wooden", "Inspiration"),
        new("The only way to do great work is to love what you do.", "Steve Jobs", "Passion"),
        new("Focus on being productive instead of busy.", "Tim Ferriss", "Productivity"),
        new("Small daily improvements over time lead to stunning results.", "Robin Sharma", "Growth"),
        new("Creativity is intelligence having fun.", "Albert Einstein", "Creativity")
    ];

    private readonly List<AffirmationItem> _quotes;
    private readonly Timer? _rotationTimer;
    private readonly object _lock = new();

    private int _currentIndex;
    private string _currentText = string.Empty;
    private string _author = string.Empty;
    private string _category = string.Empty;
    private bool _isFavorite;
    private bool _isActive = true;
    private bool _isTimerRunning;
    private bool _disposed;

    public string WidgetId { get; } = "widget-affirmations";
    public string Title => "Affirmations";
    public WidgetFamily Family => WidgetFamily.Affirmations;

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    public string CurrentText
    {
        get => _currentText;
        private set { if (_currentText != value) { _currentText = value; OnPropertyChanged(); } }
    }

    public string Author
    {
        get => _author;
        private set { if (_author != value) { _author = value; OnPropertyChanged(); } }
    }

    public string Category
    {
        get => _category;
        private set { if (_category != value) { _category = value; OnPropertyChanged(); } }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set { if (_isFavorite != value) { _isFavorite = value; OnPropertyChanged(); } }
    }

    public ICommand NextQuoteCommand { get; }
    public ICommand PreviousQuoteCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AffirmationWidgetViewModel(IEnumerable<AffirmationItem>? customQuotes = null, bool startActive = true)
    {
        _quotes = customQuotes != null ? new List<AffirmationItem>(customQuotes) : new List<AffirmationItem>(DefaultQuotes);

        NextQuoteCommand = new RelayCommand(NextQuote);
        PreviousQuoteCommand = new RelayCommand(PreviousQuote);
        ToggleFavoriteCommand = new RelayCommand(() => IsFavorite = !IsFavorite);

        DisplayQuoteAtIndex(_currentIndex);

        // 1-hour rotation
        _rotationTimer = new Timer(OnRotationTick, null, Timeout.Infinite, Timeout.Infinite);

        if (startActive)
        {
            StartTimer();
        }
    }

    public void AddQuote(string text, string author, string category = "Custom")
    {
        lock (_lock)
        {
            _quotes.Add(new AffirmationItem(text, author, category));
        }
    }

    public void NextQuote()
    {
        lock (_lock)
        {
            if (_quotes.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _quotes.Count;
            DisplayQuoteAtIndex(_currentIndex);
        }
    }

    public void PreviousQuote()
    {
        lock (_lock)
        {
            if (_quotes.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _quotes.Count) % _quotes.Count;
            DisplayQuoteAtIndex(_currentIndex);
        }
    }

    private void DisplayQuoteAtIndex(int index)
    {
        if (index >= 0 && index < _quotes.Count)
        {
            var quote = _quotes[index];
            CurrentText = $"“{quote.Text}”";
            Author = quote.Author;
            Category = quote.Category;
        }
    }

    private void OnRotationTick(object? state)
    {
        if (!_isActive || !_isTimerRunning) return;
        NextQuote();
    }

    private void StartTimer()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _isTimerRunning = true;
            _rotationTimer?.Change(TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }
    }

    private void StopTimer()
    {
        lock (_lock)
        {
            _isTimerRunning = false;
            _rotationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public void OnLauncherHidden()
    {
        // Pause-on-Hide GPU Law: Suspend quote rotation timer
        StopTimer();
    }

    public void OnLauncherShown()
    {
        StartTimer();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopTimer();
        _rotationTimer?.Dispose();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
