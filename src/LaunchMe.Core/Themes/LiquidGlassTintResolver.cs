using LaunchMe.Contracts.Spaces;

namespace LaunchMe.Core.Themes;

/// <summary>
/// Resolved visual parameters for the Liquid Glass and Flat theme rendering pipelines.
/// </summary>
public sealed record LiquidGlassResolution
{
    public required ColorRgba BackgroundTint { get; init; }
    public required ColorRgba BorderHighlight { get; init; }
    public required ColorRgba InnerShadow { get; init; }
    public required ColorRgba TitleColor { get; init; }
    public required double BlurRadius { get; init; }
    public required double Opacity { get; init; }
    public required double RefractionIndex { get; init; }
    public required bool IsLiquidGlass { get; init; }
    public required bool IsTinted { get; init; }
    public required bool IsFlat { get; init; }
}

/// <summary>
/// Resolves raw <see cref="ThemeSettings"/> into concrete brush colors, blur radii, and contrast-checked typography colors.
/// </summary>
public static class LiquidGlassTintResolver
{
    private static readonly ColorRgba ClearGlassBase = new(255, 255, 255, 30);
    private static readonly ColorRgba FlatClearBase = new(30, 30, 30, 220);

    public static LiquidGlassResolution Resolve(ThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var isLiquidGlass = !settings.Style.Equals("Flat", StringComparison.OrdinalIgnoreCase);
        var isTinted = settings.Mode.Equals("Tinted", StringComparison.OrdinalIgnoreCase);
        var clampedIntensity = Math.Clamp(settings.Intensity, 0.0, 1.0);

        ColorRgba baseColor;
        if (isTinted)
        {
            if (!ColorParser.TryParseHex(settings.TintHex, out baseColor))
            {
                baseColor = ColorRgba.DefaultAccent;
            }
        }
        else
        {
            baseColor = isLiquidGlass ? ClearGlassBase : FlatClearBase;
        }

        ColorRgba backgroundTint;
        ColorRgba borderHighlight;
        ColorRgba innerShadow;
        double blurRadius;
        double refractionIndex;
        double opacity;

        if (isLiquidGlass)
        {
            if (isTinted)
            {
                // Modulate alpha between 0.15 and 0.85 based on intensity to retain frosted glass translucency
                var alpha = Math.Clamp(clampedIntensity * 0.70 + 0.15, 0.15, 0.85);
                backgroundTint = baseColor.WithAlpha(alpha);
                opacity = alpha;
            }
            else
            {
                // Clear Liquid Glass: neutral frost with subtle white sheen
                var alpha = Math.Clamp(clampedIntensity * 0.35 + 0.05, 0.05, 0.40);
                backgroundTint = new ColorRgba(255, 255, 255, (byte)Math.Round(alpha * 255.0));
                opacity = alpha;
            }

            // Liquid glass features specular edge highlight and gaussian blur
            var borderAlpha = Math.Clamp(clampedIntensity * 0.40 + 0.10, 0.10, 0.50);
            borderHighlight = new ColorRgba(255, 255, 255, (byte)Math.Round(borderAlpha * 255.0));
            innerShadow = new ColorRgba(0, 0, 0, (byte)Math.Round(clampedIntensity * 50.0));
            blurRadius = Math.Round(20.0 + clampedIntensity * 40.0, 1); // 20px - 60px blur
            refractionIndex = Math.Round(1.10 + clampedIntensity * 0.25, 2); // 1.10 - 1.35
        }
        else
        {
            // Flat mode: opaque or high-opacity matte finish with zero blur
            if (isTinted)
            {
                var alpha = Math.Clamp(clampedIntensity * 0.50 + 0.50, 0.50, 1.0);
                backgroundTint = baseColor.WithAlpha(alpha);
                opacity = alpha;
            }
            else
            {
                var alpha = Math.Clamp(clampedIntensity * 0.40 + 0.60, 0.60, 1.0);
                backgroundTint = new ColorRgba(24, 24, 27, (byte)Math.Round(alpha * 255.0));
                opacity = alpha;
            }

            borderHighlight = new ColorRgba(255, 255, 255, 25);
            innerShadow = ColorRgba.Transparent;
            blurRadius = 0.0;
            refractionIndex = 1.0;
        }

        var titleColor = LuminanceCalculator.ResolveTitleColor(settings.TitleColor, backgroundTint);

        return new LiquidGlassResolution
        {
            BackgroundTint = backgroundTint,
            BorderHighlight = borderHighlight,
            InnerShadow = innerShadow,
            TitleColor = titleColor,
            BlurRadius = blurRadius,
            Opacity = opacity,
            RefractionIndex = refractionIndex,
            IsLiquidGlass = isLiquidGlass,
            IsTinted = isTinted,
            IsFlat = !isLiquidGlass
        };
    }
}
