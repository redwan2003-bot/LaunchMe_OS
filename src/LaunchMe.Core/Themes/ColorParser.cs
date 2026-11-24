using System.Globalization;

namespace LaunchMe.Core.Themes;

/// <summary>
/// Parser and formatter for hexadecimal and CSS color strings.
/// </summary>
public static class ColorParser
{
    /// <summary>
    /// Attempts to parse a hexadecimal color string into a <see cref="ColorRgba"/>.
    /// Supports:
    /// - 3-digit RGB: "#RGB" or "RGB"
    /// - 4-digit ARGB: "#ARGB" or "ARGB"
    /// - 6-digit RRGGBB: "#RRGGBB" or "RRGGBB"
    /// - 8-digit AARRGGBB: "#AARRGGBB" or "AARRGGBB"
    /// </summary>
    public static bool TryParseHex(string? hex, out ColorRgba color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        var span = hex.AsSpan().Trim();
        if (span.StartsWith("#"))
        {
            span = span[1..];
        }

        switch (span.Length)
        {
            case 3: // RGB -> RRGGBB (Alpha = 255)
            {
                if (!TryParseNibble(span[0], out var rNibble) ||
                    !TryParseNibble(span[1], out var gNibble) ||
                    !TryParseNibble(span[2], out var bNibble))
                {
                    return false;
                }

                var r = (byte)((rNibble << 4) | rNibble);
                var g = (byte)((gNibble << 4) | gNibble);
                var b = (byte)((bNibble << 4) | bNibble);
                color = new ColorRgba(r, g, b, 255);
                return true;
            }

            case 4: // ARGB -> AARRGGBB
            {
                if (!TryParseNibble(span[0], out var aNibble) ||
                    !TryParseNibble(span[1], out var rNibble) ||
                    !TryParseNibble(span[2], out var gNibble) ||
                    !TryParseNibble(span[3], out var bNibble))
                {
                    return false;
                }

                var a = (byte)((aNibble << 4) | aNibble);
                var r = (byte)((rNibble << 4) | rNibble);
                var g = (byte)((gNibble << 4) | gNibble);
                var b = (byte)((bNibble << 4) | bNibble);
                color = new ColorRgba(r, g, b, a);
                return true;
            }

            case 6: // RRGGBB (Alpha = 255)
            {
                if (!uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                {
                    return false;
                }

                var r = (byte)((rgb >> 16) & 0xFF);
                var g = (byte)((rgb >> 8) & 0xFF);
                var b = (byte)(rgb & 0xFF);
                color = new ColorRgba(r, g, b, 255);
                return true;
            }

            case 8: // AARRGGBB (Standard Windows/XAML hex)
            {
                if (!uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
                {
                    return false;
                }

                var a = (byte)((argb >> 24) & 0xFF);
                var r = (byte)((argb >> 16) & 0xFF);
                var g = (byte)((argb >> 8) & 0xFF);
                var b = (byte)(argb & 0xFF);
                color = new ColorRgba(r, g, b, a);
                return true;
            }

            default:
                return false;
        }
    }

    /// <summary>
    /// Parses a hexadecimal color string into a <see cref="ColorRgba"/>, throwing <see cref="FormatException"/> on invalid format.
    /// </summary>
    public static ColorRgba ParseHex(string hex)
    {
        if (!TryParseHex(hex, out var color))
        {
            throw new FormatException($"Invalid hexadecimal color format: '{hex}'. Expected #RGB, #ARGB, #RRGGBB, or #AARRGGBB.");
        }

        return color;
    }

    private static bool TryParseNibble(char c, out byte nibble)
    {
        if (c is >= '0' and <= '9')
        {
            nibble = (byte)(c - '0');
            return true;
        }
        if (c is >= 'a' and <= 'f')
        {
            nibble = (byte)(c - 'a' + 10);
            return true;
        }
        if (c is >= 'A' and <= 'F')
        {
            nibble = (byte)(c - 'A' + 10);
            return true;
        }

        nibble = 0;
        return false;
    }
}
