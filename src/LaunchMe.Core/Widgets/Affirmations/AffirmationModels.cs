namespace LaunchMe.Core.Widgets.Affirmations;

public enum AffirmationCategory
{
    Focus,
    Mindfulness,
    Productivity,
    Confidence,
    Creativity,
    Wisdom
}

public enum AffirmationWidgetSize
{
    Small,
    Medium,
    Large,
    ExtraLarge
}

public sealed record AffirmationItem
{
    public required string Id { get; init; }
    public required string Quote { get; init; }
    public required string Author { get; init; }
    public required AffirmationCategory Category { get; init; }
    public bool IsCustom { get; init; }
}

public sealed record AffirmationDisplaySettings
{
    public string FontFamily { get; init; } = "Segoe UI Variable Text";
    public double FontSize { get; init; } = 16.0;
    public string TextColorHex { get; init; } = "#FFFFFF";
    public string BackgroundColorHex { get; init; } = "#1E293B";
    public AffirmationWidgetSize Size { get; init; } = AffirmationWidgetSize.Medium;
}
