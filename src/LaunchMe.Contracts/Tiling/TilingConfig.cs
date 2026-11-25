namespace LaunchMe.Contracts.Tiling;

/// <summary>
/// Configuration parameters for the dynamic tiling window engine.
/// </summary>
public record TilingConfig
{
    public int InnerGap { get; init; } = 8;
    public int OuterGap { get; init; } = 12;
    public double MinSplitRatio { get; init; } = 0.15;
    public double MaxSplitRatio { get; init; } = 0.85;
    public double DefaultSplitRatio { get; init; } = 0.50;
    public bool DwindleSpiralEnabled { get; init; } = true;
    public bool FocusBorderEnabled { get; init; } = true;
    public int FocusBorderThickness { get; init; } = 3;
    public string FocusBorderColorHex { get; init; } = "#0078D4";
}
