using FluentAssertions;
using LaunchMe.Core.Widgets.Clock;
using Xunit;

namespace LaunchMe.Core.Tests.Widgets;

public sealed class ClockWidgetProviderTests
{
    private readonly IClockWidgetProvider _provider = new ClockWidgetProvider();

    [Fact]
    public void CalculateAnalogAngles_12OClock_ReturnsZeroDegrees()
    {
        var time = new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
        var angles = _provider.CalculateAnalogAngles(time);

        angles.HourAngleDegrees.Should().Be(0.0);
        angles.MinuteAngleDegrees.Should().Be(0.0);
        angles.SecondAngleDegrees.Should().Be(0.0);
    }

    [Fact]
    public void CalculateAnalogAngles_3OClock_ReturnsExpectedDegrees()
    {
        var time = new DateTimeOffset(2026, 11, 24, 15, 0, 0, TimeSpan.Zero);
        var angles = _provider.CalculateAnalogAngles(time);

        angles.HourAngleDegrees.Should().Be(90.0); // 3 * 30 degrees
        angles.MinuteAngleDegrees.Should().Be(0.0);
        angles.SecondAngleDegrees.Should().Be(0.0);
    }

    [Fact]
    public void CalculateAnalogAngles_6_30_ReturnsHourAndMinuteHandDegrees()
    {
        var time = new DateTimeOffset(2026, 11, 24, 6, 30, 0, TimeSpan.Zero);
        var angles = _provider.CalculateAnalogAngles(time);

        // 6.5 hours * 30 = 195 degrees
        angles.HourAngleDegrees.Should().Be(195.0);
        // 30 mins * 6 = 180 degrees
        angles.MinuteAngleDegrees.Should().Be(180.0);
        angles.SecondAngleDegrees.Should().Be(0.0);
    }

    [Fact]
    public void FormatDigitalTime_Formats12H_And_24H_Correctly()
    {
        var morning = new DateTimeOffset(2026, 11, 24, 9, 15, 30, TimeSpan.Zero);
        var evening = new DateTimeOffset(2026, 11, 24, 21, 15, 30, TimeSpan.Zero);

        var mornInfo = _provider.FormatDigitalTime(morning);
        mornInfo.Hour12.Should().Be(9);
        mornInfo.Hour24.Should().Be(9);
        mornInfo.AmPm.Should().Be("AM");

        var eveInfo = _provider.FormatDigitalTime(evening);
        eveInfo.Hour12.Should().Be(9);
        eveInfo.Hour24.Should().Be(21);
        eveInfo.AmPm.Should().Be("PM");
    }

    [Fact]
    public void GetSnapshot_ProducesValidSnapshot()
    {
        var snapshot = _provider.GetSnapshot(ClockStyle.DigitalSquared);
        snapshot.Should().NotBeNull();
        snapshot.Style.Should().Be(ClockStyle.DigitalSquared);
        snapshot.Digital.FormattedTime24H.Should().NotBeNullOrWhiteSpace();
    }
}
