using LaunchMe.Contracts.Spaces;

namespace LaunchMe.Core.Themes;

public interface IThemeEngineService
{
    LiquidGlassResolution ResolveTheme(ThemeSettings settings);
    ColorRgba ParseColor(string hex);
    bool TryParseColor(string? hex, out ColorRgba color);
    double CalculateLuminance(ColorRgba color);
    double CalculateContrastRatio(ColorRgba foreground, ColorRgba background);
    ColorRgba ResolveAutoTitleColor(ColorRgba background);
}

public sealed class ThemeEngineService : IThemeEngineService
{
    public LiquidGlassResolution ResolveTheme(ThemeSettings settings)
    {
        return LiquidGlassTintResolver.Resolve(settings);
    }

    public ColorRgba ParseColor(string hex)
    {
        return ColorParser.ParseHex(hex);
    }

    public bool TryParseColor(string? hex, out ColorRgba color)
    {
        return ColorParser.TryParseHex(hex, out color);
    }

    public double CalculateLuminance(ColorRgba color)
    {
        return LuminanceCalculator.CalculateRelativeLuminance(color);
    }

    public double CalculateContrastRatio(ColorRgba foreground, ColorRgba background)
    {
        return LuminanceCalculator.CalculateContrastRatio(foreground, background);
    }

    public ColorRgba ResolveAutoTitleColor(ColorRgba background)
    {
        return LuminanceCalculator.CalculateAutoTitleColor(background);
    }
}
