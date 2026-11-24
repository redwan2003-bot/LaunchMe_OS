using FluentAssertions;
using LaunchMe.Core.Wallpapers;
using Xunit;

namespace LaunchMe.Core.Tests.Wallpapers;

public sealed class SunTimeCalculatorTests
{
    [Fact]
    public void CalculateSolarInfo_EquinoxAtNoon_ReturnsHighElevationAndDaylight()
    {
        // March 20, 2026 ~ 12:00 PM local time in San Francisco (-07:00 PDT)
        var noon = new DateTimeOffset(2026, 3, 20, 13, 0, 0, TimeSpan.FromHours(-7));
        var info = SunTimeCalculator.CalculateSolarInfo(noon, latitude: 37.7749, longitude: -122.4194);

        info.Should().NotBeNull();
        info.IsDaylight.Should().BeTrue();
        info.ElevationDegrees.Should().BeGreaterThan(40.0);
        info.Phase.Should().BeOneOf(SunPhase.Morning, SunPhase.SolarNoon, SunPhase.Afternoon);
        info.SunriseTime.Should().BeLessThan(info.SunsetTime);
    }

    [Fact]
    public void CalculateSolarInfo_Midnight_ReturnsNegativeElevationAndNight()
    {
        var midnight = new DateTimeOffset(2026, 6, 21, 0, 30, 0, TimeSpan.FromHours(-7));
        var info = SunTimeCalculator.CalculateSolarInfo(midnight, latitude: 37.7749, longitude: -122.4194);

        info.IsDaylight.Should().BeFalse();
        info.ElevationDegrees.Should().BeLessThan(-10.0);
        info.Phase.Should().BeOneOf(SunPhase.AstronomicalNight, SunPhase.NauticalTwilight);
    }

    [Fact]
    public void GetDynamicWallpaperSliceIndex_ReturnsValidSliceWithinBounds()
    {
        const int totalSlices = 16;
        for (var hour = 0; hour < 24; hour++)
        {
            var time = new DateTimeOffset(2026, 9, 23, hour, 15, 0, TimeSpan.FromHours(-7));
            var slice = SunTimeCalculator.GetDynamicWallpaperSliceIndex(time, totalSlices);

            slice.Should().BeInRange(0, totalSlices - 1);
        }
    }

    [Fact]
    public void CalculateSolarInfo_ExtremeLatitude_HandlesPolarDayAndNightWithoutCrashing()
    {
        // Svalbard (78° N) in mid-summer (polar day) and mid-winter (polar night)
        var polarDay = new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);
        var polarNight = new DateTimeOffset(2026, 12, 21, 12, 0, 0, TimeSpan.Zero);

        var dayInfo = SunTimeCalculator.CalculateSolarInfo(polarDay, latitude: 78.22, longitude: 15.65);
        var nightInfo = SunTimeCalculator.CalculateSolarInfo(polarNight, latitude: 78.22, longitude: 15.65);

        dayInfo.ElevationDegrees.Should().BeGreaterThan(0.0);
        nightInfo.ElevationDegrees.Should().BeLessThan(0.0);
    }
}
