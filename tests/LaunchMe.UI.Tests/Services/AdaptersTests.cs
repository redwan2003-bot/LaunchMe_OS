using FluentAssertions;
using LaunchMe.Core.Widgets.Weather;
using LaunchMe.Platform.Win32.Metrics;
using LaunchMe.UI.Services;
using Moq;
using Xunit;
using CoreWeatherProvider = LaunchMe.Core.Widgets.Weather.IWeatherProvider;

namespace LaunchMe.UI.Tests.Services;

public sealed class AdaptersTests
{
    [Fact]
    public async Task Win32SystemStatsAdapter_MapsSnapshotAndConvertsNetworkToKbps()
    {
        // Arrange
        var metricsMock = new Mock<ISystemMetricsService>();
        var perf = new SystemPerformanceSnapshot(
            DateTime.UtcNow,
            new CpuMetrics(42.5, 100, 200, 300),
            new MemoryMetrics(50.0, 16L * 1024 * 1024 * 1024, 8L * 1024 * 1024 * 1024, 8L * 1024 * 1024 * 1024, 0, 0),
            new NetworkMetrics(1000, 2000, 10240.0, 2048.0, []));

        metricsMock.Setup(x => x.GetCurrentSnapshot()).Returns(perf);
        var adapter = new Win32SystemStatsAdapter(metricsMock.Object);

        // Act
        var snapshot = await adapter.GetSnapshotAsync();

        // Assert
        snapshot.CpuUsagePercent.Should().Be(42.5);
        snapshot.UsedMemoryBytes.Should().Be(8L * 1024 * 1024 * 1024);
        snapshot.TotalMemoryBytes.Should().Be(16L * 1024 * 1024 * 1024);
        snapshot.NetworkUploadKbps.Should().BeApproximately(2.0, 0.001);
        snapshot.NetworkDownloadKbps.Should().BeApproximately(10.0, 0.001);
    }

    [Fact]
    public async Task Win32SystemStatsAdapter_HonorsCancellationToken()
    {
        var metricsMock = new Mock<ISystemMetricsService>();
        var adapter = new Win32SystemStatsAdapter(metricsMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => adapter.GetSnapshotAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CoreWeatherAdapter_MapsSimulatedWeatherProviderReport()
    {
        // Arrange
        var simulated = new SimulatedWeatherProvider();
        simulated.OverrideCondition(WeatherCondition.Clear, 24.5);
        var adapter = new CoreWeatherAdapter(simulated);

        // Act
        var data = await adapter.GetWeatherDataAsync("Seattle");

        // Assert
        data.Location.Should().Be("Seattle");
        data.TemperatureC.Should().Be(24.5);
        data.Condition.Should().Be("Clear Sky");
        data.HumidityPercent.Should().Be(45);
        data.WindSpeedKmh.Should().Be(12.0);
        data.ForecastDays.Should().HaveCount(7);
        data.HighTempC.Should().BeGreaterOrEqualTo(data.TemperatureC);
        data.LowTempC.Should().BeLessOrEqualTo(data.TemperatureC);
    }

    [Fact]
    public async Task CoreWeatherAdapter_MapsCustomCoreWeatherReport()
    {
        // Arrange
        var coreMock = new Mock<CoreWeatherProvider>();
        var report = new WeatherReport
        {
            Current = new WeatherReading
            {
                City = "Tokyo",
                Timestamp = DateTimeOffset.UtcNow,
                Condition = WeatherCondition.Rain,
                Description = "Showers",
                TemperatureCelsius = 18.2,
                FeelsLikeCelsius = 17.5,
                HumidityPercent = 88.6,
                WindSpeedKmh = 21.4,
                PressureHpa = 1005.0,
                UvIndex = 1.0,
                AirQualityIndex = 18,
                PrecipitationMm = 4.2
            },
            DailyForecast =
            [
                new DailyForecastItem
                {
                    Date = new DateOnly(2026, 11, 26),
                    Condition = WeatherCondition.Cloudy,
                    Description = "Cloudy",
                    HighTempC = 20.0,
                    LowTempC = 13.0,
                    PrecipitationProbability = 0.3
                }
            ],
            HourlyForecast = []
        };

        coreMock.Setup(x => x.GetWeatherReportAsync("Tokyo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var adapter = new CoreWeatherAdapter(coreMock.Object);

        // Act
        var data = await adapter.GetWeatherDataAsync("Tokyo");

        // Assert
        data.Location.Should().Be("Tokyo");
        data.TemperatureC.Should().Be(18.2);
        data.Condition.Should().Be("Showers");
        data.HumidityPercent.Should().Be(89);
        data.WindSpeedKmh.Should().Be(21.4);
        data.HighTempC.Should().Be(20.0);
        data.LowTempC.Should().Be(13.0);
        data.ForecastDays.Should().ContainSingle();
        data.ForecastDays[0].DayName.Should().Be("Thu");
        data.ForecastDays[0].Condition.Should().Be("Cloudy");
    }
}
