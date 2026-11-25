namespace LaunchMe.Core.Widgets.Clock;

public enum ClockStyle
{
    DigitalMinimal,
    DigitalBold,
    DigitalSquared,
    AnalogClassic,
    AnalogMinimal,
    AnalogChrono,
    FlipSplitFlap,
    SwissRail,
    EditorialSerif,
    ConcentricRing,
    NeonBinary,
    WorldDualTime
}

public sealed record DigitalClockInfo
{
    public required string FormattedTime12H { get; init; }
    public required string FormattedTime24H { get; init; }
    public required string FormattedDate { get; init; }
    public required string ShortDate { get; init; }
    public required string DayOfWeek { get; init; }
    public required string AmPm { get; init; }
    public required int Hour12 { get; init; }
    public required int Hour24 { get; init; }
    public required int Minute { get; init; }
    public required int Second { get; init; }
    public required int Millisecond { get; init; }
}

public sealed record AnalogClockInfo
{
    public required double HourAngleDegrees { get; init; }
    public required double MinuteAngleDegrees { get; init; }
    public required double SecondAngleDegrees { get; init; }
    public required bool IsContinuous { get; init; }
}

public sealed record ClockSnapshot
{
    public required DateTimeOffset Timestamp { get; init; }
    public required ClockStyle Style { get; init; }
    public required DigitalClockInfo Digital { get; init; }
    public required AnalogClockInfo Analog { get; init; }
}
