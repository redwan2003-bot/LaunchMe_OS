using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Models;

namespace LaunchMe.UI.Widgets.Weather;

public enum WeatherStyle
{
    Minimal,
    Compact,
    Forecast,
    Panel,
    Almanac,
    Horizon,
    Bold,
    Story,
    EditorialGlass
}

public sealed class ForecastItemViewModel
{
    public string DayName { get; }
    public string TemperatureRange { get; }
    public string ConditionGlyph { get; }

    public ForecastItemViewModel(string dayName, string temperatureRange, string glyph)
    {
        DayName = dayName;
        TemperatureRange = temperatureRange;
        ConditionGlyph = glyph;
    }
}

public sealed class WeatherWidgetViewModel : IWidgetViewModel
{
    private readonly IWeatherProvider _provider;
    private readonly Timer? _refreshTimer;
    private readonly object _lock = new();

    private string _location = "Cupertino";
    private double _temperatureCelsius = 22.0;
    private bool _isFahrenheit;
    private string _conditionText = "Sunny";
    private string _conditionGlyph = "\uE706";
    private string _highLowFormatted = "H:26° L:16°";
    private string _humidityFormatted = "52%";
    private string _windSpeedFormatted = "12 km/h";
    private WeatherStyle _style = WeatherStyle.Forecast;
    private bool _isActive = true;
    private bool _isTimerRunning;
    private bool _disposed;

    public string WidgetId { get; } = "widget-weather";
    public string Title => "Weather";
    public WidgetFamily Family => WidgetFamily.Weather;

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    public string Location
    {
        get => _location;
        set { if (_location != value) { _location = value; OnPropertyChanged(); _ = RefreshAsync(); } }
    }

    public double TemperatureCelsius
    {
        get => _temperatureCelsius;
        set
        {
            if (Math.Abs(_temperatureCelsius - value) > 0.01)
            {
                _temperatureCelsius = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentTemperatureFormatted));
                OnPropertyChanged(nameof(TemperatureFahrenheit));
            }
        }
    }

    public double TemperatureFahrenheit => (TemperatureCelsius * 9.0 / 5.0) + 32.0;

    public bool IsFahrenheit
    {
        get => _isFahrenheit;
        set
        {
            if (_isFahrenheit != value)
            {
                _isFahrenheit = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentTemperatureFormatted));
                OnPropertyChanged(nameof(HighLowFormatted));
            }
        }
    }

    public string CurrentTemperatureFormatted =>
        IsFahrenheit ? $"{Math.Round(TemperatureFahrenheit)}°F" : $"{Math.Round(TemperatureCelsius)}°C";

    public string ConditionText
    {
        get => _conditionText;
        private set { if (_conditionText != value) { _conditionText = value; OnPropertyChanged(); } }
    }

    public string ConditionGlyph
    {
        get => _conditionGlyph;
        private set { if (_conditionGlyph != value) { _conditionGlyph = value; OnPropertyChanged(); } }
    }

    public string HighLowFormatted
    {
        get => _highLowFormatted;
        private set { if (_highLowFormatted != value) { _highLowFormatted = value; OnPropertyChanged(); } }
    }

    public string HumidityFormatted
    {
        get => _humidityFormatted;
        private set { if (_humidityFormatted != value) { _humidityFormatted = value; OnPropertyChanged(); } }
    }

    public string WindSpeedFormatted
    {
        get => _windSpeedFormatted;
        private set { if (_windSpeedFormatted != value) { _windSpeedFormatted = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<ForecastItemViewModel> Forecast { get; } = new();

    public WeatherStyle Style
    {
        get => _style;
        set { if (_style != value) { _style = value; OnPropertyChanged(); } }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CycleStyleCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public WeatherWidgetViewModel(IWeatherProvider? provider = null, bool startActive = true)
    {
        _provider = provider ?? new DefaultWeatherProvider();
        RefreshCommand = new RelayCommand(() => _ = RefreshAsync());
        CycleStyleCommand = new RelayCommand(() =>
        {
            var values = Enum.GetValues<WeatherStyle>();
            Style = values[((int)Style + 1) % values.Length];
        });

        _ = RefreshAsync();

        // 30 minute periodic refresh
        _refreshTimer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);

        if (startActive)
        {
            StartTimer();
        }
    }

    private void OnTimerTick(object? state)
    {
        if (!_isActive || !_isTimerRunning) return;
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            var data = await _provider.GetWeatherDataAsync(_location);
            UpdateFromData(data);
        }
        catch
        {
            // Silently keep cached data on transient network failure
        }
    }

    public void UpdateFromData(WeatherData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        TemperatureCelsius = data.TemperatureC;
        ConditionText = data.Condition;
        ConditionGlyph = ResolveConditionGlyph(data.Condition);

        HighLowFormatted = IsFahrenheit
            ? $"H:{Math.Round((data.HighTempC * 9 / 5) + 32)}° L:{Math.Round((data.LowTempC * 9 / 5) + 32)}°"
            : $"H:{Math.Round(data.HighTempC)}° L:{Math.Round(data.LowTempC)}°";

        HumidityFormatted = $"{data.HumidityPercent}%";
        WindSpeedFormatted = $"{Math.Round(data.WindSpeedKmh)} km/h";

        Forecast.Clear();
        foreach (var day in data.ForecastDays)
        {
            string range = IsFahrenheit
                ? $"{Math.Round((day.HighTempC * 9 / 5) + 32)}° / {Math.Round((day.LowTempC * 9 / 5) + 32)}°"
                : $"{Math.Round(day.HighTempC)}° / {Math.Round(day.LowTempC)}°";

            Forecast.Add(new ForecastItemViewModel(day.DayName, range, ResolveConditionGlyph(day.Condition)));
        }
    }

    public static string ResolveConditionGlyph(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return "\uE706";

        string lower = condition.ToLowerInvariant();
        if (lower.Contains("sun") || lower.Contains("clear")) return "\uE706"; // Sunny
        if (lower.Contains("rain") || lower.Contains("shower") || lower.Contains("drizzle")) return "\uE707"; // Rain
        if (lower.Contains("thunder") || lower.Contains("storm")) return "\uE709"; // Storm
        if (lower.Contains("snow") || lower.Contains("ice") || lower.Contains("flurry")) return "\uE708"; // Snow
        if (lower.Contains("cloud") || lower.Contains("overcast")) return "\uE753"; // Cloudy
        if (lower.Contains("fog") || lower.Contains("mist") || lower.Contains("haze")) return "\uE754"; // Fog

        return "\uE706";
    }

    private void StartTimer()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _isTimerRunning = true;
            _refreshTimer?.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
        }
    }

    private void StopTimer()
    {
        lock (_lock)
        {
            _isTimerRunning = false;
            _refreshTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public void OnLauncherHidden()
    {
        // Pause-on-Hide GPU Law: Suspend weather poll
        StopTimer();
    }

    public void OnLauncherShown()
    {
        StartTimer();
        _ = RefreshAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopTimer();
        _refreshTimer?.Dispose();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
