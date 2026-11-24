namespace LaunchMe.UI.Widgets.Weather;

public sealed class ForecastDay
{
    public string DayName { get; set; } = string.Empty;
    public double HighTempC { get; set; }
    public double LowTempC { get; set; }
    public string Condition { get; set; } = "Sunny";
}

public sealed class WeatherData
{
    public string Location { get; set; } = "San Francisco";
    public double TemperatureC { get; set; } = 21.0;
    public double HighTempC { get; set; } = 24.0;
    public double LowTempC { get; set; } = 16.0;
    public string Condition { get; set; } = "Partly Cloudy";
    public int HumidityPercent { get; set; } = 55;
    public double WindSpeedKmh { get; set; } = 14.5;
    public List<ForecastDay> ForecastDays { get; set; } = new();
}

public interface IWeatherProvider
{
    Task<WeatherData> GetWeatherDataAsync(string location, CancellationToken ct = default);
}

public sealed class DefaultWeatherProvider : IWeatherProvider
{
    public Task<WeatherData> GetWeatherDataAsync(string location, CancellationToken ct = default)
    {
        var data = new WeatherData
        {
            Location = string.IsNullOrWhiteSpace(location) ? "Local Area" : location,
            TemperatureC = 21.5,
            HighTempC = 25.0,
            LowTempC = 16.0,
            Condition = "Partly Cloudy",
            HumidityPercent = 58,
            WindSpeedKmh = 12.0,
            ForecastDays =
            [
                new() { DayName = "Mon", HighTempC = 24.0, LowTempC = 15.0, Condition = "Sunny" },
                new() { DayName = "Tue", HighTempC = 22.0, LowTempC = 14.0, Condition = "Cloudy" },
                new() { DayName = "Wed", HighTempC = 19.0, LowTempC = 13.0, Condition = "Rain" },
                new() { DayName = "Thu", HighTempC = 23.0, LowTempC = 15.0, Condition = "Partly Cloudy" }
            ]
        };
        return Task.FromResult(data);
    }
}
