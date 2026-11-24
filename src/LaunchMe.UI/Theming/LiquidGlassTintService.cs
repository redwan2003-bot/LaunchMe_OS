using System.Globalization;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using LaunchMe.Contracts.Spaces;

namespace LaunchMe.UI.Theming;

/// <summary>
/// Service responsible for parsing theme configurations and calculating Liquid Glass acrylic
/// and custom tint overlay rendering properties.
/// </summary>
public sealed class LiquidGlassTintService
{
    private static readonly Color DefaultTintColor = Color.FromArgb(255, 30, 64, 175); // #1E40AF
    private static readonly Color DarkTitleColor = Color.FromArgb(255, 17, 24, 39);   // #111827
    private static readonly Color LightTitleColor = Color.FromArgb(255, 249, 250, 251); // #F9FAFB

    /// <summary>
    /// Resolves ThemeSettings into fully calculated visual state for WinUI 3 rendering.
    /// </summary>
    public ThemeVisualState ResolveVisualState(ThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var state = new ThemeVisualState();
        UpdateVisualState(state, settings);
        return state;
    }

    /// <summary>
    /// Updates an existing ThemeVisualState based on new ThemeSettings.
    /// </summary>
    public void UpdateVisualState(ThemeVisualState state, ThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(settings);

        bool isLiquidGlass = string.Equals(settings.Style, "LiquidGlass", StringComparison.OrdinalIgnoreCase);
        bool isTinted = string.Equals(settings.Mode, "Tinted", StringComparison.OrdinalIgnoreCase);
        double intensity = Math.Clamp(settings.Intensity, 0.0, 1.0);

        Color baseColor = ParseHexColor(settings.TintHex, DefaultTintColor);

        state.IsLiquidGlass = isLiquidGlass;
        state.IsTinted = isTinted;
        state.TintColor = baseColor;
        state.TintOpacity = intensity;

        if (isLiquidGlass)
        {
            state.BlurIntensity = 20.0 + (intensity * 30.0); // 20px to 50px blur
            byte alpha = isTinted ? (byte)(intensity * 210) : (byte)(intensity * 75);
            Color backdropColor = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

            state.BackdropColor = backdropColor;
            state.BackdropBrush = ThemeVisualState.TryCreateBrush(backdropColor);
            state.TintBrush = ThemeVisualState.TryCreateBrush(Color.FromArgb((byte)(intensity * 255), baseColor.R, baseColor.G, baseColor.B));
        }
        else
        {
            // Flat mode
            state.BlurIntensity = 0.0;
            byte alpha = (byte)(intensity * 255);
            Color flatColor = isTinted 
                ? Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B) 
                : Color.FromArgb(alpha, 15, 23, 42); // slate-900

            state.BackdropColor = flatColor;
            state.BackdropBrush = ThemeVisualState.TryCreateBrush(flatColor);
            state.TintBrush = ThemeVisualState.TryCreateBrush(flatColor);
        }

        // Title color resolution (Auto vs explicit)
        var (titleColor, titleHex) = ResolveTitleColor(settings.TitleColor, baseColor, intensity, isTinted);
        state.EffectiveTitleColor = titleColor;
        state.EffectiveTitleHex = titleHex;
        state.TitleBrush = ThemeVisualState.TryCreateBrush(titleColor);
    }

    /// <summary>
    /// Parses a HEX color string supporting formats #RGB, #RGBA, #RRGGBB, #AARRGGBB.
    /// </summary>
    public static Color ParseHexColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;

        string clean = hex.Trim().TrimStart('#');
        try
        {
            if (clean.Length == 3) // RGB -> RRGGBB
            {
                byte r = byte.Parse(new string(clean[0], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte g = byte.Parse(new string(clean[1], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte b = byte.Parse(new string(clean[2], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return Color.FromArgb(255, r, g, b);
            }
            if (clean.Length == 4) // RGBA -> AARRGGBB
            {
                byte r = byte.Parse(new string(clean[0], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte g = byte.Parse(new string(clean[1], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte b = byte.Parse(new string(clean[2], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte a = byte.Parse(new string(clean[3], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return Color.FromArgb(a, r, g, b);
            }
            if (clean.Length == 6) // RRGGBB
            {
                byte r = byte.Parse(clean[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte g = byte.Parse(clean[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte b = byte.Parse(clean[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return Color.FromArgb(255, r, g, b);
            }
            if (clean.Length == 8) // AARRGGBB
            {
                byte a = byte.Parse(clean[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte r = byte.Parse(clean[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte g = byte.Parse(clean[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte b = byte.Parse(clean[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return Color.FromArgb(a, r, g, b);
            }
        }
        catch
        {
            // Invalid hex format, return fallback
        }

        return fallback;
    }

    /// <summary>
    /// Calculates perceived luminance using standard ITU-R BT.601 coefficients (0.0 to 1.0).
    /// </summary>
    public static double CalculateLuminance(Color color)
    {
        return ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255.0;
    }

    private static (Color Color, string Hex) ResolveTitleColor(string? titleColorSetting, Color tintColor, double intensity, bool isTinted)
    {
        if (!string.IsNullOrWhiteSpace(titleColorSetting) &&
            !string.Equals(titleColorSetting, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            Color explicitColor = ParseHexColor(titleColorSetting, LightTitleColor);
            return (explicitColor, $"#{explicitColor.A:X2}{explicitColor.R:X2}{explicitColor.G:X2}{explicitColor.B:X2}");
        }

        // Auto luminance calculation
        // In Liquid Glass, effective background luminance blends tint color with a dark workspace backdrop.
        double tintLum = CalculateLuminance(tintColor);
        double effectiveLum = isTinted ? (tintLum * intensity) : 0.15; // untinted glass is dark tinted translucent

        if (effectiveLum > 0.55)
        {
            // High luminance background -> dark text
            return (DarkTitleColor, "#111827");
        }

        // Low luminance background -> light text
        return (LightTitleColor, "#F9FAFB");
    }
}
