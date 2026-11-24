using System.Text.RegularExpressions;
using LaunchMe.Contracts.Clipboard;

namespace LaunchMe.Core.Clipboard;

public static class ClipboardClassifier
{
    private static readonly Regex HexColorRegex = new(
        @"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
        RegexOptions.Compiled);

    private static readonly Regex RgbColorRegex = new(
        @"^rgba?\(\s*\d+\s*,\s*\d+\s*,\s*\d+(\s*,\s*[\d.]+\s*)?\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] CodeKeywords =
    [
        "public class ", "namespace ", "using System", "import ", "def ", "function ",
        "const ", "let ", "var ", "return ", "SELECT ", "INSERT INTO ", "CREATE TABLE ",
        "<html>", "<div", "<script", "<?php", "#include <", "fn main()", "async ", "await "
    ];

    public static ClipboardCategory Classify(string? text, IReadOnlyList<string>? filePaths, bool hasBitmap, bool isScreenshot = false)
    {
        if (isScreenshot)
        {
            return ClipboardCategory.Screenshots;
        }

        if (hasBitmap)
        {
            return ClipboardCategory.Images;
        }

        if (filePaths != null && filePaths.Count > 0)
        {
            return ClipboardCategory.Files;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return ClipboardCategory.Text;
        }

        string trimmed = text.Trim();

        if (HexColorRegex.IsMatch(trimmed) || RgbColorRegex.IsMatch(trimmed))
        {
            return ClipboardCategory.Colors;
        }

        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            return ClipboardCategory.Code;
        }

        foreach (var keyword in CodeKeywords)
        {
            if (trimmed.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return ClipboardCategory.Code;
            }
        }

        return ClipboardCategory.Text;
    }
}
