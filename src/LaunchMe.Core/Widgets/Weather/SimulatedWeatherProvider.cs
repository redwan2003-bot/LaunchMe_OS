namespace LaunchMe.Core.Widgets.Weather;

public interface IWeatherProvider
{
    Task<WeatherReport> GetWeatherReportAsync(string city = "Cupertino", CancellationToken ct = default);
    Task<WeatherReading> GetCurrentReadingAsync(string city = "Cupertino", CancellationToken ct = default);
    void OverrideCondition(WeatherCondition condition, double? temperatureC = null);
    void ClearOverride();
}

public sealed class SimulatedWeatherProvider : IWeatherProvider
{
    private WeatherCondition? _overriddenCondition;
    private double? _overriddenTempC;

    public void OverrideCondition(WeatherCondition condition, double? temperatureC = null)
    {
        _overriddenCondition = condition;
        _overriddenTempC = temperatureC;
    }

    public void ClearOverride()
    {
        _overriddenCondition = null;
        _overriddenTempC = null;
    }

    public Task<WeatherReading> GetCurrentReadingAsync(string city = "Cupertino", CancellationToken ct = default)
    {
        var reading = GenerateReading(city, DateTimeOffset.Now);
        return Task.FromResult(reading);
    }

    public Task<WeatherReport> GetWeatherReportAsync(string city = "Cupertino", CancellationToken ct = default)
    {
        var now = DateTimeOffset.Now;
        var current = GenerateReading(city, now);

        var hourly = new List<HourlyForecastItem>(24);
        for (var i = 1; i <= 24; i++)
        {
            var forecastTime = now.AddHours(i);
            var readingAtHour = GenerateReading(city, forecastTime);
            hourly.Add(new HourlyForecastItem
            {
                Time = forecastTime,
                Condition = readingAtHour.Condition,
                TemperatureC = readingAtHour.TemperatureCelsius,
                PrecipitationProbability = CalculatePrecipitationProbability(readingAtHour.Condition)
            });
        }

        var daily = new List<DailyForecastItem>(7);
        var baseDate = DateOnly.FromDateTime(now.DateTime);
        for (var d = 1; d <= 7; d++)
        {
            var forecastDate = baseDate.AddDays(d);
            var condition = PickDailyCondition(d, current.Condition);
            var (high, low) = CalculateDailyRange(current.TemperatureCelsius, d);

            daily.Add(new DailyForecastItem
            {
                Date = forecastDate,
                Condition = condition,
                Description = GetConditionDescription(condition),
                HighTempC = high,
                LowTempC = low,
                PrecipitationProbability = CalculatePrecipitationProbability(condition)
            });
        }

        var report = new WeatherReport
        {
            Current = current,
            DailyForecast = daily,
            HourlyForecast = hourly
        };

        return Task.FromResult(report);
    }

    private WeatherReading GenerateReading(string city, DateTimeOffset timestamp)
    {
        var hour = timestamp.Hour;
        // Diurnal temperature swing: lowest around 05:00, peak around 15:00
        var diurnalOffset = Math.Sin((hour - 9) / 24.0 * 2.0 * Math.PI) * 4.5;
        var baseTemp = _overriddenTempC ?? (21.0 + diurnalOffset);

        var condition = _overriddenCondition ?? PickConditionFromHourAndMonth(timestamp);
        var (humidity, wind, pressure, uv, aqi, precip) = DerivePhysicalMetrics(condition, hour, baseTemp);

        return new WeatherReading
        {
            City = city,
            Timestamp = timestamp,
            Condition = condition,
            Description = GetConditionDescription(condition),
            TemperatureCelsius = Math.Round(baseTemp, 1),
            FeelsLikeCelsius = Math.Round(baseTemp + (humidity > 70 ? 1.5 : -0.8), 1),
            HumidityPercent = Math.Round(humidity, 1),
            WindSpeedKmh = Math.Round(wind, 1),
            PressureHpa = Math.Round(pressure, 1),
            UvIndex = Math.Round(uv, 1),
            AirQualityIndex = aqi,
            PrecipitationMm = Math.Round(precip, 1)
        };
    }

    private static WeatherCondition PickConditionFromHourAndMonth(DateTimeOffset timestamp)
    {
        var month = timestamp.Month;
        var day = timestamp.Day;
        var hash = (month * 31 + day + timestamp.Hour) % 10;

        return hash switch
        {
            0 or 1 or 2 => WeatherCondition.Clear,
            3 or 4 => WeatherCondition.PartlyCloudy,
            5 => WeatherCondition.Cloudy,
            6 => WeatherCondition.Overcast,
            7 => WeatherCondition.LightRain,
            8 => WeatherCondition.Rain,
            9 => WeatherCondition.Windy,
            _ => WeatherCondition.Clear
        };
    }

    private static WeatherCondition PickDailyCondition(int dayOffset, WeatherCondition baseCondition)
    {
        var conditions = Enum.GetValues<WeatherCondition>();
        var idx = ((int)baseCondition + dayOffset) % conditions.Length;
        return conditions[idx];
    }

    private static (double high, double low) CalculateDailyRange(double baseTemp, int dayOffset)
    {
        var high = Math.Round(baseTemp + 3.0 + (dayOffset % 3) * 0.5, 1);
        var low = Math.Round(baseTemp - 5.0 - (dayOffset % 2) * 0.5, 1);
        return (high, low);
    }

    private static (double humidity, double wind, double pressure, double uv, int aqi, double precip) DerivePhysicalMetrics(
        WeatherCondition condition, int hour, double temp)
    {
        double humidity;
        double wind;
        double pressure;
        double uv;
        int aqi;
        double precip;

        var isDay = hour is >= 7 and <= 19;
        var baseUv = isDay ? Math.Max(0.0, Math.Sin((hour - 7) / 12.0 * Math.PI) * 8.0) : 0.0;

        switch (condition)
        {
            case WeatherCondition.Clear:
                humidity = 45.0;
                wind = 12.0;
                pressure = 1018.0;
                uv = baseUv;
                aqi = 25;
                precip = 0.0;
                break;

            case WeatherCondition.PartlyCloudy:
                humidity = 55.0;
                wind = 14.0;
                pressure = 1015.0;
                uv = baseUv * 0.75;
                aqi = 30;
                precip = 0.0;
                break;

            case WeatherCondition.Cloudy:
            case WeatherCondition.Overcast:
                humidity = 70.0;
                wind = 16.0;
                pressure = 1012.0;
                uv = baseUv * 0.35;
                aqi = 40;
                precip = 0.0;
                break;

            case WeatherCondition.Fog:
                humidity = 95.0;
                wind = 6.0;
                pressure = 1014.0;
                uv = baseUv * 0.2;
                aqi = 50;
                precip = 0.2;
                break;

            case WeatherCondition.LightRain:
                humidity = 82.0;
                wind = 18.0;
                pressure = 1008.0;
                uv = baseUv * 0.25;
                aqi = 20;
                precip = 1.8;
                break;

            case WeatherCondition.Rain:
            case WeatherCondition.HeavyRain:
                humidity = 90.0;
                wind = 25.0;
                pressure = 1004.0;
                uv = baseUv * 0.15;
                aqi = 15;
                precip = 6.5;
                break;

            case WeatherCondition.Thunderstorm:
                humidity = 92.0;
                wind = 38.0;
                pressure = 998.0;
                uv = baseUv * 0.1;
                aqi = 18;
                precip = 14.2;
                break;

            case WeatherCondition.Snow:
                humidity = 85.0;
                wind = 15.0;
                pressure = 1010.0;
                uv = baseUv * 0.3;
                aqi = 15;
                precip = 3.0;
                break;

            case WeatherCondition.Windy:
                humidity = 48.0;
                wind = 42.0;
                pressure = 1011.0;
                uv = baseUv * 0.8;
                aqi = 35;
                precip = 0.0;
                break;

            default:
                humidity = 50.0;
                wind = 10.0;
                pressure = 1013.25;
                uv = baseUv;
                aqi = 30;
                precip = 0.0;
                break;
        }

        return (humidity, wind, pressure, uv, aqi, precip);
    }

    private static double CalculatePrecipitationProbability(WeatherCondition condition)
    {
        return condition switch
        {
            WeatherCondition.Clear => 0.05,
            WeatherCondition.PartlyCloudy => 0.15,
            WeatherCondition.Cloudy => 0.30,
            WeatherCondition.Overcast => 0.45,
            WeatherCondition.Fog => 0.25,
            WeatherCondition.LightRain => 0.75,
            WeatherCondition.Rain => 0.90,
            WeatherCondition.HeavyRain => 0.98,
            WeatherCondition.Thunderstorm => 0.95,
            WeatherCondition.Snow => 0.85,
            WeatherCondition.Windy => 0.20,
            _ => 0.10
        };
    }

    private static string GetConditionDescription(WeatherCondition condition)
    {
        return condition switch
        {
            WeatherCondition.Clear => "Clear Sky",
            WeatherCondition.PartlyCloudy => "Partly Cloudy",
            WeatherCondition.Cloudy => "Cloudy",
            WeatherCondition.Overcast => "Overcast",
            WeatherCondition.Fog => "Dense Fog",
            WeatherCondition.LightRain => "Light Rain",
            WeatherCondition.Rain => "Showers",
            WeatherCondition.HeavyRain => "Heavy Downpour",
            WeatherCondition.Thunderstorm => "Severe Thunderstorm",
            WeatherCondition.Snow => "Flurries & Snow",
            WeatherCondition.Windy => "High Winds",
            _ => "Clear"
        };
    }
}
