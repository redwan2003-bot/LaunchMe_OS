namespace LaunchMe.Core.Widgets.Weather;

public enum WeatherCondition
{
    Clear,
    PartlyCloudy,
    Cloudy,
    Overcast,
    Fog,
    LightRain,
    Rain,
    HeavyRain,
    Thunderstorm,
    Snow,
    Windy
}

public sealed record WeatherReading
{
    public required string City { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required WeatherCondition Condition { get; init; }
    public required string Description { get; init; }
    public required double TemperatureCelsius { get; init; }
    public double TemperatureFahrenheit => Math.Round(TemperatureCelsius * 9.0 / 5.0 + 32.0, 1);
    public required double FeelsLikeCelsius { get; init; }
    public double FeelsLikeFahrenheit => Math.Round(FeelsLikeCelsius * 9.0 / 5.0 + 32.0, 1);
    public required double HumidityPercent { get; init; }
    public required double WindSpeedKmh { get; init; }
    public double WindSpeedMph => Math.Round(WindSpeedKmh * 0.621371, 1);
    public required double PressureHpa { get; init; }
    public required double UvIndex { get; init; }
    public required int AirQualityIndex { get; init; }
    public required double PrecipitationMm { get; init; }
}

public sealed record DailyForecastItem
{
    public required DateOnly Date { get; init; }
    public required WeatherCondition Condition { get; init; }
    public required string Description { get; init; }
    public required double HighTempC { get; init; }
    public required double LowTempC { get; init; }
    public double HighTempF => Math.Round(HighTempC * 9.0 / 5.0 + 32.0, 1);
    public double LowTempF => Math.Round(LowTempC * 9.0 / 5.0 + 32.0, 1);
    public required double PrecipitationProbability { get; init; }
}

public sealed record HourlyForecastItem
{
    public required DateTimeOffset Time { get; init; }
    public required WeatherCondition Condition { get; init; }
    public required double TemperatureC { get; init; }
    public double TemperatureF => Math.Round(TemperatureC * 9.0 / 5.0 + 32.0, 1);
    public required double PrecipitationProbability { get; init; }
}

public sealed record WeatherReport
{
    public required WeatherReading Current { get; init; }
    public required IReadOnlyList<DailyForecastItem> DailyForecast { get; init; }
    public required IReadOnlyList<HourlyForecastItem> HourlyForecast { get; init; }
}
