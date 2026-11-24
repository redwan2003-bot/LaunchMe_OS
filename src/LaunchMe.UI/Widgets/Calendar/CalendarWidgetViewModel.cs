using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using LaunchMe.UI.Theming;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Models;

namespace LaunchMe.UI.Widgets.Calendar;

public sealed class CalendarDayViewModel : INotifyPropertyChanged
{
    private static readonly Color ActiveTodayColor = Color.FromArgb(255, 56, 189, 248); // #38BDF8
    private static readonly Color TransparentColor = Color.FromArgb(0, 0, 0, 0);
    private static readonly Color CurrentMonthTextColor = Color.FromArgb(255, 255, 255, 255);
    private static readonly Color OtherMonthTextColor = Color.FromArgb(100, 148, 163, 184); // muted

    private bool _isSelected;

    public DateTime Date { get; }
    public int DayNumber => Date.Day;
    public bool IsCurrentMonth { get; }
    public bool IsToday { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); OnPropertyChanged(nameof(BackgroundBrush)); } }
    }

    public Color TextColor => IsCurrentMonth ? CurrentMonthTextColor : OtherMonthTextColor;
    public Color BackgroundColor => (IsToday || IsSelected) ? ActiveTodayColor : TransparentColor;

    public SolidColorBrush? TextBrush => ThemeVisualState.TryCreateBrush(TextColor);
    public SolidColorBrush? BackgroundBrush => ThemeVisualState.TryCreateBrush(BackgroundColor);

    public event PropertyChangedEventHandler? PropertyChanged;

    public CalendarDayViewModel(DateTime date, bool isCurrentMonth, bool isToday)
    {
        Date = date;
        IsCurrentMonth = isCurrentMonth;
        IsToday = isToday;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class CalendarWidgetViewModel : IWidgetViewModel
{
    public static readonly IReadOnlyList<string> DayOfWeekHeaders = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];

    private DateTime _viewMonth;
    private DateTime _selectedDate;
    private bool _isActive = true;

    public string WidgetId { get; } = "widget-calendar";
    public string Title => "Calendar";
    public WidgetFamily Family => WidgetFamily.Calendar;

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    public DateTime ViewMonth
    {
        get => _viewMonth;
        set
        {
            if (_viewMonth != value)
            {
                _viewMonth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MonthYearTitle));
                PopulateCalendarDays();
            }
        }
    }

    public string MonthYearTitle => ViewMonth.ToString("MMMM yyyy");

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate != value)
            {
                _selectedDate = value;
                OnPropertyChanged();
                UpdateSelection();
            }
        }
    }

    public ObservableCollection<CalendarDayViewModel> Days { get; } = new();

    public ICommand NextMonthCommand { get; }
    public ICommand PreviousMonthCommand { get; }
    public ICommand GoToTodayCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CalendarWidgetViewModel(DateTime? initialDate = null)
    {
        DateTime today = initialDate ?? DateTime.Today;
        _selectedDate = today;
        _viewMonth = new DateTime(today.Year, today.Month, 1);

        NextMonthCommand = new RelayCommand(() => ViewMonth = ViewMonth.AddMonths(1));
        PreviousMonthCommand = new RelayCommand(() => ViewMonth = ViewMonth.AddMonths(-1));
        GoToTodayCommand = new RelayCommand(() =>
        {
            SelectedDate = DateTime.Today;
            ViewMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        });

        PopulateCalendarDays();
    }

    public void PopulateCalendarDays()
    {
        Days.Clear();
        DateTime today = DateTime.Today;

        DateTime firstOfMonth = new DateTime(ViewMonth.Year, ViewMonth.Month, 1);
        int dayOfWeek = (int)firstOfMonth.DayOfWeek;
        // Adjust for Monday start (Monday = 0, Sunday = 6)
        int offset = (dayOfWeek == 0) ? 6 : dayOfWeek - 1;

        DateTime startDate = firstOfMonth.AddDays(-offset);

        // Fill 6 rows * 7 columns = 42 days
        for (int i = 0; i < 42; i++)
        {
            DateTime date = startDate.AddDays(i);
            bool isCurrentMonth = date.Month == ViewMonth.Month;
            bool isToday = date.Date == today.Date;

            var dayVm = new CalendarDayViewModel(date, isCurrentMonth, isToday)
            {
                IsSelected = (date.Date == SelectedDate.Date)
            };

            Days.Add(dayVm);
        }
    }

    private void UpdateSelection()
    {
        foreach (var day in Days)
        {
            day.IsSelected = (day.Date.Date == SelectedDate.Date);
        }
    }

    public void OnLauncherHidden()
    {
        // No heavy timers needed for calendar
    }

    public void OnLauncherShown()
    {
        // Refresh today marker if date changed while hidden
        PopulateCalendarDays();
    }

    public void Dispose()
    {
        Days.Clear();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
