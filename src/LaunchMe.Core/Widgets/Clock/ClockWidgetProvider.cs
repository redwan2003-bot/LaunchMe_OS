using System.Globalization;

namespace LaunchMe.Core.Widgets.Clock;

public interface IClockWidgetProvider
{
    ClockSnapshot GetSnapshot(
        ClockStyle style = ClockStyle.DigitalMinimal,
        DateTimeOffset? time = null,
        bool smoothSeconds = false);

    AnalogClockInfo CalculateAnalogAngles(DateTimeOffset time, bool smoothSeconds = false);
    DigitalClockInfo FormatDigitalTime(DateTimeOffset time);
}

public sealed class ClockWidgetProvider : IClockWidgetProvider
{
    public ClockSnapshot GetSnapshot(
        ClockStyle style = ClockStyle.DigitalMinimal,
        DateTimeOffset? time = null,
        bool smoothSeconds = false)
    {
        var timestamp = time ?? DateTimeOffset.Now;
        var digital = FormatDigitalTime(timestamp);
        var analog = CalculateAnalogAngles(timestamp, smoothSeconds);

        return new ClockSnapshot
        {
            Timestamp = timestamp,
            Style = style,
            Digital = digital,
            Analog = analog
        };
    }

    public AnalogClockInfo CalculateAnalogAngles(DateTimeOffset time, bool smoothSeconds = false)
    {
        var hour = time.Hour % 12;
        var minute = time.Minute;
        var second = time.Second;
        var millisecond = time.Millisecond;

        // Hour hand angle: 30 degrees per hour + 0.5 degrees per minute + (0.5/60) per second
        var hourAngle = (hour + (minute / 60.0) + (second / 3600.0)) * 30.0;

        // Minute hand angle: 6 degrees per minute + 0.1 degrees per second
        var minuteAngle = (minute + (second / 60.0) + (smoothSeconds ? millisecond / 60000.0 : 0.0)) * 6.0;

        // Second hand angle: 6 degrees per second
        var secondFraction = smoothSeconds ? (second + millisecond / 1000.0) : second;
        var secondAngle = secondFraction * 6.0;

        return new AnalogClockInfo
        {
            HourAngleDegrees = Math.Round(hourAngle % 360.0, 2),
            MinuteAngleDegrees = Math.Round(minuteAngle % 360.0, 2),
            SecondAngleDegrees = Math.Round(secondAngle % 360.0, 2),
            IsContinuous = smoothSeconds
        };
    }

    public DigitalClockInfo FormatDigitalTime(DateTimeOffset time)
    {
        var hour24 = time.Hour;
        var hour12 = hour24 % 12;
        if (hour12 == 0) hour12 = 12;

        return new DigitalClockInfo
        {
            FormattedTime12H = time.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture),
            FormattedTime24H = time.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            FormattedDate = time.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture),
            ShortDate = time.ToString("MMM d, yyyy", CultureInfo.InvariantCulture),
            DayOfWeek = time.ToString("dddd", CultureInfo.InvariantCulture),
            AmPm = time.ToString("tt", CultureInfo.InvariantCulture),
            Hour12 = hour12,
            Hour24 = hour24,
            Minute = time.Minute,
            Second = time.Second,
            Millisecond = time.Millisecond
        };
    }
}
