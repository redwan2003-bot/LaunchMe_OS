using System.Globalization;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;

namespace LaunchMe.UI.Services;

/// <summary>
/// Manages LaunchMe-Win's 18-language localization resources with automatic
/// WinRT <see cref="ResourceLoader"/> lookup and XML fallback for unpackaged/headless runs.
/// </summary>
public sealed class LocalizationService
{
    public static readonly IReadOnlyList<string> SupportedCultures =
    [
        "en-US", "fr-FR", "de-DE", "es-ES", "it-IT",
        "pt-BR", "ja-JP", "zh-CN", "zh-TW", "ko-KR",
        "ar-SA", "ru-RU", "hi-IN", "nl-NL", "pl-PL",
        "tr-TR", "sv-SE", "da-DK"
    ];

    private string _currentCulture;
    private readonly Dictionary<string, string> _fallbackStrings = new(StringComparer.OrdinalIgnoreCase);

    public string CurrentCulture => _currentCulture;

    public bool IsRightToLeft =>
        _currentCulture.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    public LocalizationService(string? culture = null)
    {
        _currentCulture = ResolveCulture(culture ?? CultureInfo.CurrentUICulture.Name);
        LoadFallbackResw(_currentCulture);
    }

    public void SetCulture(string culture)
    {
        _currentCulture = ResolveCulture(culture);
        LoadFallbackResw(_currentCulture);
    }

    public string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        if (_fallbackStrings.TryGetValue(key, out var localized) && !string.IsNullOrEmpty(localized))
        {
            return localized;
        }

        if (IsPackagedProcess())
        {
            try
            {
                var loader = ResourceLoader.GetForViewIndependentUse();
                var value = loader.GetString(key);
                if (!string.IsNullOrEmpty(value)) return value;
            }
            catch
            {
            }
        }

        return key;
    }

    private static bool IsPackagedProcess()
    {
        try
        {
            _ = Package.Current;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveCulture(string requested)
    {
        if (SupportedCultures.Contains(requested, StringComparer.OrdinalIgnoreCase))
        {
            return SupportedCultures.First(c => c.Equals(requested, StringComparison.OrdinalIgnoreCase));
        }

        var lang = requested.Split('-')[0];
        var match = SupportedCultures.FirstOrDefault(c => c.StartsWith(lang + "-", StringComparison.OrdinalIgnoreCase));
        return match ?? "en-US";
    }

    private void LoadFallbackResw(string culture)
    {
        _fallbackStrings.Clear();
        string? path = FindReswPath(culture);
        if (path == null) return;

        try
        {
            var doc = XDocument.Load(path);
            foreach (var data in doc.Descendants("data"))
            {
                string? name = data.Attribute("name")?.Value;
                string? val = data.Element("value")?.Value;
                if (!string.IsNullOrWhiteSpace(name) && val != null)
                {
                    _fallbackStrings[name] = val;
                }
            }
        }
        catch
        {
        }
    }

    private static string? FindReswPath(string culture)
    {
        string local = Path.Combine(AppContext.BaseDirectory, "Strings", culture, "Resources.resw");
        if (File.Exists(local)) return local;

        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
        {
            string candidate = Path.Combine(dir, "src", "LaunchMe.UI", "Strings", culture, "Resources.resw");
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}
