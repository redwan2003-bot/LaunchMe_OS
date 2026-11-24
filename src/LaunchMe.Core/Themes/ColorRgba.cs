using System.Globalization;

namespace LaunchMe.Core.Themes;

/// <summary>
/// Immutable RGBA 32-bit color representation.
/// </summary>
public readonly struct ColorRgba : IEquatable<ColorRgba>
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public ColorRgba(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public ColorRgba(float r, float g, float b, float a = 1.0f)
    {
        R = (byte)Math.Clamp((int)Math.Round(r * 255f), 0, 255);
        G = (byte)Math.Clamp((int)Math.Round(g * 255f), 0, 255);
        B = (byte)Math.Clamp((int)Math.Round(b * 255f), 0, 255);
        A = (byte)Math.Clamp((int)Math.Round(a * 255f), 0, 255);
    }

    public static ColorRgba Transparent => new(0, 0, 0, 0);
    public static ColorRgba White => new(255, 255, 255, 255);
    public static ColorRgba Black => new(0, 0, 0, 255);
    public static ColorRgba DefaultAccent => new(30, 64, 175, 255); // #1E40AF (Blue 800)
    public static ColorRgba LightTitle => new(249, 250, 251, 255); // #F9FAFB
    public static ColorRgba DarkTitle => new(17, 24, 39, 255); // #111827

    public ColorRgba WithAlpha(byte alpha) => new(R, G, B, alpha);

    public ColorRgba WithAlpha(double alphaPercent)
    {
        var clamped = Math.Clamp(alphaPercent, 0.0, 1.0);
        return new ColorRgba(R, G, B, (byte)Math.Round(clamped * 255.0));
    }

    /// <summary>
    /// Alpha-blends this color over a background color.
    /// </summary>
    public ColorRgba BlendOver(ColorRgba background)
    {
        if (A == 255) return this;
        if (A == 0) return background;

        var aSrc = A / 255.0;
        var aDst = background.A / 255.0;
        var aOut = aSrc + aDst * (1.0 - aSrc);

        if (aOut <= 0.0001) return Transparent;

        var r = (R * aSrc + background.R * aDst * (1.0 - aSrc)) / aOut;
        var g = (G * aSrc + background.G * aDst * (1.0 - aSrc)) / aOut;
        var b = (B * aSrc + background.B * aDst * (1.0 - aSrc)) / aOut;

        return new ColorRgba(
            (byte)Math.Clamp((int)Math.Round(r), 0, 255),
            (byte)Math.Clamp((int)Math.Round(g), 0, 255),
            (byte)Math.Clamp((int)Math.Round(b), 0, 255),
            (byte)Math.Clamp((int)Math.Round(aOut * 255.0), 0, 255));
    }

    public string ToHex(bool includeAlpha = false, bool argbOrder = true)
    {
        if (!includeAlpha)
        {
            return string.Create(CultureInfo.InvariantCulture, $"#{R:X2}{G:X2}{B:X2}");
        }

        return argbOrder
            ? string.Create(CultureInfo.InvariantCulture, $"#{A:X2}{R:X2}{G:X2}{B:X2}")
            : string.Create(CultureInfo.InvariantCulture, $"#{R:X2}{G:X2}{B:X2}{A:X2}");
    }

    public string ToRgbaCss()
    {
        var a = Math.Round(A / 255.0, 3);
        return string.Create(CultureInfo.InvariantCulture, $"rgba({R}, {G}, {B}, {a})");
    }

    public override string ToString() => ToHex(includeAlpha: A < 255);

    public bool Equals(ColorRgba other) => R == other.R && G == other.G && B == other.B && A == other.A;

    public override bool Equals(object? obj) => obj is ColorRgba other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    public static bool operator ==(ColorRgba left, ColorRgba right) => left.Equals(right);

    public static bool operator !=(ColorRgba left, ColorRgba right) => !left.Equals(right);
}
