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

    /// <summary>
    /// Returns a normalized copy of <see cref="ThemeSettings"/> with intensity and specular intensity clamped to [0.0, 1.0],
    /// preserving <see cref="ThemeSettings.EnableGrain"/>.
    /// </summary>
    public static ThemeSettings NormalizeSettings(ThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new ThemeSettings
        {
            Style = string.IsNullOrWhiteSpace(settings.Style) ? "LiquidGlass" : settings.Style,
            Mode = string.IsNullOrWhiteSpace(settings.Mode) ? "Tinted" : settings.Mode,
            TintHex = string.IsNullOrWhiteSpace(settings.TintHex) ? "#1E40AF" : settings.TintHex,
            Intensity = double.IsNaN(settings.Intensity)
                ? LiquidGlassTintResolver.DefaultIntensity
                : Math.Clamp(settings.Intensity, 0.0, 1.0),
            TitleColor = string.IsNullOrWhiteSpace(settings.TitleColor) ? "Auto" : settings.TitleColor,
            SpecularIntensity = LiquidGlassTintResolver.ClampSpecularIntensity(settings.SpecularIntensity),
            EnableGrain = settings.EnableGrain
        };
    }

    public static double ClampSpecularIntensity(double specularIntensity)
    {
        return LiquidGlassTintResolver.ClampSpecularIntensity(specularIntensity);
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
