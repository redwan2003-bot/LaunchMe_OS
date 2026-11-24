using FluentAssertions;
using LaunchMe.Core.Widgets.Weather;
using Xunit;

namespace LaunchMe.Core.Tests.Widgets;

public sealed class SimulatedWeatherProviderTests
{
    private readonly IWeatherProvider _provider = new SimulatedWeatherProvider();

    [Fact]
    public async Task GetCurrentReadingAsync_ReturnsRealisticValues()
    {
        var reading = await _provider.GetCurrentReadingAsync("San Francisco");

        reading.Should().NotBeNull();
        reading.City.Should().Be("San Francisco");
        reading.TemperatureCelsius.Should().BeInRange(-30.0, 50.0);
        reading.TemperatureFahrenheit.Should().BeInRange(-22.0, 122.0);
        reading.HumidityPercent.Should().BeInRange(0.0, 100.0);
        reading.WindSpeedKmh.Should().BeGreaterThanOrEqualTo(0.0);
        reading.PressureHpa.Should().BeInRange(900.0, 1050.0);
    }

    [Fact]
    public async Task GetWeatherReportAsync_ProducesHourlyAndDailyForecasts()
    {
        var report = await _provider.GetWeatherReportAsync("Seattle");

        report.Current.Should().NotBeNull();
        report.HourlyForecast.Should().HaveCount(24);
        report.DailyForecast.Should().HaveCount(7);

        foreach (var item in report.DailyForecast)
        {
            item.HighTempC.Should().BeGreaterThanOrEqualTo(item.LowTempC);
            item.PrecipitationProbability.Should().BeInRange(0.0, 1.0);
        }
    }

    [Fact]
    public async Task OverrideCondition_OverridesCurrentWeather()
    {
        _provider.OverrideCondition(WeatherCondition.Snow, -5.0);

        var reading = await _provider.GetCurrentReadingAsync("Denver");
        reading.Condition.Should().Be(WeatherCondition.Snow);
        reading.TemperatureCelsius.Should().Be(-5.0);
        reading.PrecipitationMm.Should().BeGreaterThan(0.0);

        _provider.ClearOverride();
        var normalReading = await _provider.GetCurrentReadingAsync("Denver");
        normalReading.Should().NotBeNull();
    }
}
