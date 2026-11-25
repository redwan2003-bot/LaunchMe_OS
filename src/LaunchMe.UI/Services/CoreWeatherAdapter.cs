using System.Globalization;
using LaunchMe.Core.Widgets.Weather;
using LaunchMe.UI.Widgets.Weather;
using CoreWeatherProvider = LaunchMe.Core.Widgets.Weather.IWeatherProvider;
using UiWeatherProvider = LaunchMe.UI.Widgets.Weather.IWeatherProvider;

namespace LaunchMe.UI.Services;

/// <summary>
/// Adapts <see cref="CoreWeatherProvider"/> weather reports into UI <see cref="WeatherData"/> models.
/// </summary>
public sealed class CoreWeatherAdapter : UiWeatherProvider
{
    private readonly CoreWeatherProvider _coreProvider;

    public CoreWeatherAdapter(CoreWeatherProvider? coreProvider = null)
    {
        _coreProvider = coreProvider ?? new SimulatedWeatherProvider();
    }

    public async Task<WeatherData> GetWeatherDataAsync(string location, CancellationToken ct = default)
    {
        string city = string.IsNullOrWhiteSpace(location) ? "Cupertino" : location.Trim();
        WeatherReport report = await _coreProvider.GetWeatherReportAsync(city, ct).ConfigureAwait(false);

        double highTemp = report.DailyForecast.Count > 0
            ? Math.Max(report.Current.TemperatureCelsius, report.DailyForecast[0].HighTempC)
            : Math.Round(report.Current.TemperatureCelsius + 3.0, 1);

        double lowTemp = report.DailyForecast.Count > 0
            ? Math.Min(report.Current.TemperatureCelsius, report.DailyForecast[0].LowTempC)
            : Math.Round(report.Current.TemperatureCelsius - 5.0, 1);

        var forecastDays = report.DailyForecast
            .Select(d => new ForecastDay
            {
                DayName = d.Date.ToString("ddd", CultureInfo.InvariantCulture),
                HighTempC = d.HighTempC,
                LowTempC = d.LowTempC,
                Condition = d.Description
            })
            .ToList();

        return new WeatherData
        {
            Location = report.Current.City,
            TemperatureC = report.Current.TemperatureCelsius,
            HighTempC = highTemp,
            LowTempC = lowTemp,
            Condition = report.Current.Description,
            HumidityPercent = (int)Math.Round(report.Current.HumidityPercent),
            WindSpeedKmh = report.Current.WindSpeedKmh,
            ForecastDays = forecastDays
        };
    }
}
