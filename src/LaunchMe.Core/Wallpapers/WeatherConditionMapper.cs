namespace LaunchMe.Core.Wallpapers;

/// <summary>
/// Maps real-time weather conditions to dynamic wallpaper visual states and asset file patterns.
/// </summary>
public static class WeatherConditionMapper
{
    private static readonly Dictionary<string, string> ConditionAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sunny"] = "Clear",
        ["clear"] = "Clear",
        ["fair"] = "Clear",
        ["partlycloudy"] = "PartlyCloudy",
        ["partly cloudy"] = "PartlyCloudy",
        ["mostly sunny"] = "PartlyCloudy",
        ["mostly clear"] = "PartlyCloudy",
        ["cloudy"] = "Cloudy",
        ["overcast"] = "Overcast",
        ["fog"] = "Fog",
        ["mist"] = "Fog",
        ["haze"] = "Fog",
        ["smoke"] = "Fog",
        ["drizzle"] = "Rain",
        ["light rain"] = "Rain",
        ["rain"] = "Rain",
        ["showers"] = "Rain",
        ["heavy rain"] = "HeavyRain",
        ["thunderstorm"] = "Thunderstorm",
        ["storm"] = "Thunderstorm",
        ["snow"] = "Snow",
        ["light snow"] = "Snow",
        ["heavy snow"] = "Snow",
        ["blizzard"] = "Snow",
        ["sleet"] = "Snow",
        ["wind"] = "Windy",
        ["windy"] = "Windy",
        ["breezy"] = "Windy"
    };

    public static IReadOnlyList<string> SupportedConditions =>
    [
        "Clear",
        "PartlyCloudy",
        "Cloudy",
        "Overcast",
        "Fog",
        "Rain",
        "HeavyRain",
        "Thunderstorm",
        "Snow",
        "Windy"
    ];

    /// <summary>
    /// Normalizes any raw weather string into a standard condition category.
    /// </summary>
    public static string NormalizeCondition(string? rawCondition)
    {
        if (string.IsNullOrWhiteSpace(rawCondition))
        {
            return "Clear";
        }

        var trimmed = rawCondition.Trim();
        if (ConditionAliases.TryGetValue(trimmed, out var normalized))
        {
            return normalized;
        }

        // Fuzzy fallback search
        foreach (var (key, value) in ConditionAliases)
        {
            if (trimmed.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return "Clear";
    }

    /// <summary>
    /// Generates candidate file names for a dynamic weather wallpaper asset (e.g. "rain_day.jpg", "rain.jpg").
    /// </summary>
    public static IReadOnlyList<string> GetCandidateFileNames(string condition, bool isNight)
    {
        var normalized = NormalizeCondition(condition);
        var timeOfDay = isNight ? "night" : "day";
        var lowerCond = normalized.ToLowerInvariant();

        return
        [
            $"{lowerCond}_{timeOfDay}.jpg",
            $"{lowerCond}_{timeOfDay}.png",
            $"{lowerCond}_{timeOfDay}.mp4",
            $"{lowerCond}.jpg",
            $"{lowerCond}.png",
            $"{lowerCond}.mp4",
            $"weather_{lowerCond}_{timeOfDay}.jpg",
            $"weather_{lowerCond}.jpg"
        ];
    }

    /// <summary>
    /// Resolves the actual asset file path within a directory given the current weather condition and daylight state.
    /// </summary>
    public static string? ResolveWeatherAsset(string directoryPath, string rawCondition, bool isNight)
    {
        if (!Directory.Exists(directoryPath))
        {
            return null;
        }

        var candidates = GetCandidateFileNames(rawCondition, isNight);
        foreach (var candidate in candidates)
        {
            var fullPath = Path.Combine(directoryPath, candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        // Fallback: look for any image in the directory
        var firstImage = Directory.EnumerateFiles(directoryPath)
            .FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                 f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                 f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

        return firstImage;
    }
}
