using FluentAssertions;
using LaunchMe.Core.Wallpapers;
using Xunit;

namespace LaunchMe.Core.Tests.Wallpapers;

public sealed class WeatherConditionMapperTests
{
    [Theory]
    [InlineData("Sunny", "Clear")]
    [InlineData("clear sky", "Clear")]
    [InlineData("partly cloudy", "PartlyCloudy")]
    [InlineData("scattered showers", "Rain")]
    [InlineData("heavy snow", "Snow")]
    [InlineData("severe thunderstorm", "Thunderstorm")]
    [InlineData("dense fog", "Fog")]
    [InlineData("gusty wind", "Windy")]
    public void NormalizeCondition_VariousDescriptions_MapsToCanonical(string raw, string expected)
    {
        var normalized = WeatherConditionMapper.NormalizeCondition(raw);
        normalized.Should().Be(expected);
    }

    [Fact]
    public void GetCandidateFileNames_DayAndNight_ProducesDifferentiatedNames()
    {
        var dayNames = WeatherConditionMapper.GetCandidateFileNames("Rain", isNight: false);
        var nightNames = WeatherConditionMapper.GetCandidateFileNames("Rain", isNight: true);

        dayNames.Should().Contain("rain_day.jpg");
        nightNames.Should().Contain("rain_night.jpg");
    }

    [Fact]
    public void ResolveWeatherAsset_WithNonExistentDir_ReturnsNull()
    {
        var result = WeatherConditionMapper.ResolveWeatherAsset("C:\\non_existent_folder_xyz", "Clear", false);
        result.Should().BeNull();
    }
}
