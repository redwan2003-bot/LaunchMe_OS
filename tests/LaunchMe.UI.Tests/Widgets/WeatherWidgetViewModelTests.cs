using FluentAssertions;
using LaunchMe.UI.Widgets.Weather;
using Moq;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

public sealed class WeatherWidgetViewModelTests
{
    private readonly Mock<IWeatherProvider> _weatherProviderMock = new();

    [Fact]
    public async Task Refresh_UpdatesWeatherAndForecastData()
    {
        // Arrange
        var testData = new WeatherData
        {
            Location = "Tokyo",
            TemperatureC = 18.0,
            HighTempC = 20.0,
            LowTempC = 12.0,
            Condition = "Rain",
            HumidityPercent = 82,
            WindSpeedKmh = 15.0,
            ForecastDays =
            [
                new() { DayName = "Tue", HighTempC = 19.0, LowTempC = 13.0, Condition = "Cloudy" },
                new() { DayName = "Wed", HighTempC = 21.0, LowTempC = 14.0, Condition = "Sunny" }
            ]
        };
        _weatherProviderMock.Setup(x => x.GetWeatherDataAsync("Tokyo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        using var vm = new WeatherWidgetViewModel(_weatherProviderMock.Object, startActive: false);

        // Act
        vm.Location = "Tokyo";
        await vm.RefreshAsync();

        // Assert
        vm.TemperatureCelsius.Should().Be(18.0);
        vm.CurrentTemperatureFormatted.Should().Be("18°C");
        vm.ConditionText.Should().Be("Rain");
        vm.ConditionGlyph.Should().Be("\uE707"); // Rain icon
        vm.HumidityFormatted.Should().Be("82%");
        vm.Forecast.Should().HaveCount(2);
    }

    [Fact]
    public void FahrenheitToggle_ConvertsTemperatureAccurately()
    {
        // Arrange
        using var vm = new WeatherWidgetViewModel(startActive: false)
        {
            TemperatureCelsius = 25.0
        };

        // Act
        vm.IsFahrenheit = true;

        // Assert: 25 * 9/5 + 32 = 77°F
        vm.TemperatureFahrenheit.Should().Be(77.0);
        vm.CurrentTemperatureFormatted.Should().Be("77°F");
    }

    [Theory]
    [InlineData("Sunny", "\uE706")]
    [InlineData("Thunderstorm", "\uE709")]
    [InlineData("Snow flurry", "\uE708")]
    [InlineData("Cloudy sky", "\uE753")]
    [InlineData("Dense fog", "\uE754")]
    public void ResolveConditionGlyph_MapsConditionsToExpectedGlyphs(string condition, string expectedGlyph)
    {
        // Act
        string glyph = WeatherWidgetViewModel.ResolveConditionGlyph(condition);

        // Assert
        glyph.Should().Be(expectedGlyph);
    }
}
