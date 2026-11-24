using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LaunchMe.Contracts.Media;
using LaunchMe.UI.Widgets.Affirmations;
using LaunchMe.UI.Widgets.Calendar;
using LaunchMe.UI.Widgets.Clock;
using LaunchMe.UI.Widgets.Models;
using LaunchMe.UI.Widgets.Music;
using LaunchMe.UI.Widgets.Pomodoro;
using LaunchMe.UI.Widgets.SystemStats;
using LaunchMe.UI.Widgets.Weather;

namespace LaunchMe.UI.Widgets.Catalog;

public sealed class WidgetCatalogViewModel : INotifyPropertyChanged, IDisposable
{
    private bool _disposed;

    public ClockWidgetViewModel Clock { get; }
    public WeatherWidgetViewModel Weather { get; }
    public MusicWidgetViewModel Music { get; }
    public SystemStatsWidgetViewModel SystemStats { get; }
    public PomodoroWidgetViewModel Pomodoro { get; }
    public AffirmationWidgetViewModel Affirmations { get; }
    public CalendarWidgetViewModel Calendar { get; }

    public ObservableCollection<IWidgetViewModel> Widgets { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public WidgetCatalogViewModel(
        IMediaSessionService mediaService,
        IWeatherProvider? weatherProvider = null,
        ISystemStatsProvider? systemStatsProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mediaService);

        Clock = new ClockWidgetViewModel();
        Weather = new WeatherWidgetViewModel(weatherProvider);
        Music = new MusicWidgetViewModel(mediaService);
        SystemStats = new SystemStatsWidgetViewModel(systemStatsProvider);
        Pomodoro = new PomodoroWidgetViewModel();
        Affirmations = new AffirmationWidgetViewModel();
        Calendar = new CalendarWidgetViewModel();

        Widgets.Add(Clock);
        Widgets.Add(Weather);
        Widgets.Add(Music);
        Widgets.Add(SystemStats);
        Widgets.Add(Pomodoro);
        Widgets.Add(Affirmations);
        Widgets.Add(Calendar);
    }

    /// <summary>
    /// Broadcasts launcher hidden event to all active widgets to enforce 0% idle GPU/CPU.
    /// </summary>
    public void OnLauncherHidden()
    {
        foreach (var widget in Widgets)
        {
            widget.OnLauncherHidden();
        }
    }

    /// <summary>
    /// Broadcasts launcher shown event to all active widgets to resume timers.
    /// </summary>
    public void OnLauncherShown()
    {
        foreach (var widget in Widgets)
        {
            widget.OnLauncherShown();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var widget in Widgets)
        {
            widget.Dispose();
        }
        Widgets.Clear();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
