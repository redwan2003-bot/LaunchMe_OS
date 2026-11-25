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
    public void GetSnapshot_ProducesValidSnapshot_ForAllTwelveClockStyles()
    {
        var allStyles = Enum.GetValues<ClockStyle>();
        allStyles.Should().HaveCount(12);
        allStyles.Should().Contain([
            ClockStyle.DigitalMinimal,
            ClockStyle.DigitalBold,
            ClockStyle.DigitalSquared,
            ClockStyle.AnalogClassic,
            ClockStyle.AnalogMinimal,
            ClockStyle.AnalogChrono,
            ClockStyle.FlipSplitFlap,
            ClockStyle.SwissRail,
            ClockStyle.EditorialSerif,
            ClockStyle.ConcentricRing,
            ClockStyle.NeonBinary,
            ClockStyle.WorldDualTime
        ]);

        var fixedTime = new DateTimeOffset(2026, 11, 25, 14, 27, 45, TimeSpan.Zero);
        var displayNames = new HashSet<string>();

        foreach (var style in allStyles)
        {
            var snapshot = _provider.GetSnapshot(style, fixedTime);
            snapshot.Should().NotBeNull();
            snapshot.Style.Should().Be(style);
            snapshot.Timestamp.Should().Be(fixedTime);
            snapshot.Digital.FormattedTime24H.Should().Be("14:27:45");

            var displayName = _provider.GetStyleDisplayName(style);
            displayName.Should().NotBeNullOrWhiteSpace();
            displayNames.Add(displayName).Should().BeTrue();
        }
    }

    [Fact]
    public void GetBinaryTimeMatrix_EncodesBcdDigitsCorrectly()
    {
        // 21:45:09 -> digits [2, 1, 4, 5, 0, 9]
        var time = new DateTimeOffset(2026, 11, 25, 21, 45, 9, TimeSpan.Zero);
        var matrix = _provider.GetBinaryTimeMatrix(time);

        matrix.GetLength(0).Should().Be(4);
        matrix.GetLength(1).Should().Be(6);

        // Col 0: 2 (0010)
        AssertColumnBits(matrix, 0, [false, false, true, false]);
        // Col 1: 1 (0001)
        AssertColumnBits(matrix, 1, [false, false, false, true]);
        // Col 2: 4 (0100)
        AssertColumnBits(matrix, 2, [false, true, false, false]);
        // Col 3: 5 (0101)
        AssertColumnBits(matrix, 3, [false, true, false, true]);
        // Col 4: 0 (0000)
        AssertColumnBits(matrix, 4, [false, false, false, false]);
        // Col 5: 9 (1001)
        AssertColumnBits(matrix, 5, [true, false, false, true]);
    }

    private static void AssertColumnBits(bool[,] matrix, int col, bool[] expectedBits)
    {
        for (var row = 0; row < 4; row++)
        {
            matrix[row, col].Should().Be(expectedBits[row], $"row {row}, col {col}");
        }
    }
}
