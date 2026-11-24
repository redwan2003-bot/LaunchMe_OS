namespace LaunchMe.Core.Themes;

/// <summary>
/// WCAG 2.1 relative luminance and contrast ratio calculator for adaptive UI coloring.
/// </summary>
public static class LuminanceCalculator
{
    /// <summary>
    /// Calculates the relative luminance of an sRGB color per WCAG 2.1 specifications (0.0 = darkest black, 1.0 = brightest white).
    /// </summary>
    public static double CalculateRelativeLuminance(ColorRgba color)
    {
        var rLin = LinearizeChannel(color.R / 255.0);
        var gLin = LinearizeChannel(color.G / 255.0);
        var bLin = LinearizeChannel(color.B / 255.0);

        return 0.2126 * rLin + 0.7152 * gLin + 0.0722 * bLin;
    }

    /// <summary>
    /// Calculates WCAG 2.1 contrast ratio between two colors (range: 1.0 to 21.0).
    /// </summary>
    public static double CalculateContrastRatio(ColorRgba foreground, ColorRgba background)
    {
        var l1 = CalculateRelativeLuminance(foreground);
        var l2 = CalculateRelativeLuminance(background);

        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);

        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Determines the optimal title/text color (dark vs light) based on the background luminance and contrast ratio.
    /// </summary>
    public static ColorRgba CalculateAutoTitleColor(
        ColorRgba background,
        ColorRgba? preferredDark = null,
        ColorRgba? preferredLight = null)
    {
        var dark = preferredDark ?? ColorRgba.DarkTitle;
        var light = preferredLight ?? ColorRgba.LightTitle;

        // If background has transparency, blend over default dark desktop background for contrast calculation
        var effectiveBg = background.A < 255 ? background.BlendOver(new ColorRgba(18, 18, 18, 255)) : background;

        var contrastWithDark = CalculateContrastRatio(dark, effectiveBg);
        var contrastWithLight = CalculateContrastRatio(light, effectiveBg);

        return contrastWithLight >= contrastWithDark ? light : dark;
    }

    /// <summary>
    /// Resolves the title color string from settings ("Auto", "Light", "Dark", or a hex code) against the background tint.
    /// </summary>
    public static ColorRgba ResolveTitleColor(string? titleColorSetting, ColorRgba background)
    {
        if (string.IsNullOrWhiteSpace(titleColorSetting) ||
            titleColorSetting.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return CalculateAutoTitleColor(background);
        }

        if (titleColorSetting.Equals("Light", StringComparison.OrdinalIgnoreCase) ||
            titleColorSetting.Equals("White", StringComparison.OrdinalIgnoreCase))
        {
            return ColorRgba.LightTitle;
        }

        if (titleColorSetting.Equals("Dark", StringComparison.OrdinalIgnoreCase) ||
            titleColorSetting.Equals("Black", StringComparison.OrdinalIgnoreCase))
        {
            return ColorRgba.DarkTitle;
        }

        if (ColorParser.TryParseHex(titleColorSetting, out var parsed))
        {
            return parsed;
        }

        // Fallback to auto
        return CalculateAutoTitleColor(background);
    }

    private static double LinearizeChannel(double channel)
    {
        return channel <= 0.04045
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
