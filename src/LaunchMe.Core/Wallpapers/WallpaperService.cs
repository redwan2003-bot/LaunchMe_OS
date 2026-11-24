using LaunchMe.Contracts.Spaces;

namespace LaunchMe.Core.Wallpapers;

public interface IWallpaperService
{
    ResolvedWallpaper Resolve(
        WallpaperSettings settings,
        bool isLauncherVisible,
        DateTimeOffset? currentTime = null,
        string? currentWeatherCondition = null,
        string? musicArtworkPath = null,
        string? baseDirectory = null);

    SolarInfo GetSolarInfo(DateTimeOffset? time = null, double lat = 37.7749, double lon = -122.4194);
    int GetDynamicSliceIndex(DateTimeOffset? time = null, int totalSlices = 16, double lat = 37.7749, double lon = -122.4194);
    string? ResolveWeatherAsset(string directoryPath, string condition, bool isNight);
    bool IsSupportedAsset(string filePath, WallpaperType type);
}

public sealed class WallpaperService : IWallpaperService
{
    public static readonly IReadOnlyList<string> SupportedImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".webp",
        ".heic"
    ];

    public ResolvedWallpaper Resolve(
        WallpaperSettings settings,
        bool isLauncherVisible,
        DateTimeOffset? currentTime = null,
        string? currentWeatherCondition = null,
        string? musicArtworkPath = null,
        string? baseDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var now = currentTime ?? DateTimeOffset.Now;
        var blur = Math.Clamp(settings.Blur, 0.0, 1.0);

        return settings.Type switch
        {
            WallpaperType.Static => ResolveStatic(settings.Asset, blur, baseDirectory),
            WallpaperType.Dynamic => ResolveDynamic(settings.Asset, blur, now, currentWeatherCondition, baseDirectory),
            WallpaperType.LiveVideo => ResolveLiveVideo(settings.Asset, blur, isLauncherVisible, settings.PauseOnHide, baseDirectory),
            WallpaperType.MusicArt => ResolveMusicArt(musicArtworkPath, blur),
            _ => ResolveStatic(settings.Asset, blur, baseDirectory)
        };
    }

    public SolarInfo GetSolarInfo(DateTimeOffset? time = null, double lat = 37.7749, double lon = -122.4194)
    {
        return SunTimeCalculator.CalculateSolarInfo(time ?? DateTimeOffset.Now, lat, lon);
    }

    public int GetDynamicSliceIndex(DateTimeOffset? time = null, int totalSlices = 16, double lat = 37.7749, double lon = -122.4194)
    {
        return SunTimeCalculator.GetDynamicWallpaperSliceIndex(time ?? DateTimeOffset.Now, totalSlices, lat, lon);
    }

    public string? ResolveWeatherAsset(string directoryPath, string condition, bool isNight)
    {
        return WeatherConditionMapper.ResolveWeatherAsset(directoryPath, condition, isNight);
    }

    public bool IsSupportedAsset(string filePath, WallpaperType type)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;

        var ext = Path.GetExtension(filePath);
        return type switch
        {
            WallpaperType.LiveVideo => VideoAssetResolver.IsSupportedVideo(filePath),
            WallpaperType.Static or WallpaperType.Dynamic or WallpaperType.MusicArt =>
                SupportedImageExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static ResolvedWallpaper ResolveStatic(string rawAsset, double blur, string? baseDirectory)
    {
        var resolvedPath = ResolvePath(rawAsset, baseDirectory);
        var exists = !string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath);

        return new ResolvedWallpaper
        {
            Type = WallpaperType.Static,
            ResolvedAssetPath = resolvedPath ?? string.Empty,
            Exists = exists,
            Blur = blur,
            ShouldPause = false,
            PlaybackState = VideoPlaybackState.Disabled,
            FallbackAssetPath = null,
            DetectedMimeType = exists && resolvedPath != null ? GetImageMimeType(resolvedPath) : null
        };
    }

    private static ResolvedWallpaper ResolveDynamic(
        string rawAsset,
        double blur,
        DateTimeOffset now,
        string? weatherCondition,
        string? baseDirectory)
    {
        var resolvedPath = ResolvePath(rawAsset, baseDirectory);

        // If the asset is a directory, resolve dynamic weather or solar slice from directory
        if (!string.IsNullOrWhiteSpace(resolvedPath) && Directory.Exists(resolvedPath))
        {
            var solar = SunTimeCalculator.CalculateSolarInfo(now);
            var isNight = !solar.IsDaylight;

            string? chosenFile = null;
            if (!string.IsNullOrWhiteSpace(weatherCondition))
            {
                chosenFile = WeatherConditionMapper.ResolveWeatherAsset(resolvedPath, weatherCondition, isNight);
            }

            if (chosenFile == null)
            {
                var slice = SunTimeCalculator.GetDynamicWallpaperSliceIndex(now, 16);
                var candidate = Path.Combine(resolvedPath, $"slice_{slice:D2}.jpg");
                if (File.Exists(candidate))
                {
                    chosenFile = candidate;
                }
            }

            if (chosenFile != null)
            {
                resolvedPath = chosenFile;
            }
        }

        var exists = !string.IsNullOrWhiteSpace(resolvedPath) && (File.Exists(resolvedPath) || Directory.Exists(resolvedPath));

        return new ResolvedWallpaper
        {
            Type = WallpaperType.Dynamic,
            ResolvedAssetPath = resolvedPath ?? string.Empty,
            Exists = exists,
            Blur = blur,
            ShouldPause = false,
            PlaybackState = VideoPlaybackState.Disabled,
            FallbackAssetPath = null,
            DetectedMimeType = exists && resolvedPath != null ? GetImageMimeType(resolvedPath) : null
        };
    }

    private static ResolvedWallpaper ResolveLiveVideo(
        string rawAsset,
        double blur,
        bool isLauncherVisible,
        bool pauseOnHide,
        string? baseDirectory)
    {
        var resolvedPath = VideoAssetResolver.ResolveVideoPath(rawAsset, baseDirectory);
        var exists = !string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath);
        var playbackState = VideoAssetResolver.EvaluatePlaybackState(isLauncherVisible, pauseOnHide, exists);

        return new ResolvedWallpaper
        {
            Type = WallpaperType.LiveVideo,
            ResolvedAssetPath = resolvedPath ?? string.Empty,
            Exists = exists,
            Blur = blur,
            ShouldPause = playbackState == VideoPlaybackState.Paused,
            PlaybackState = playbackState,
            FallbackAssetPath = null,
            DetectedMimeType = exists && resolvedPath != null ? VideoAssetResolver.DetectMimeType(resolvedPath) : null
        };
    }

    private static ResolvedWallpaper ResolveMusicArt(string? artworkPath, double blur)
    {
        var exists = !string.IsNullOrWhiteSpace(artworkPath) && File.Exists(artworkPath);

        return new ResolvedWallpaper
        {
            Type = WallpaperType.MusicArt,
            ResolvedAssetPath = artworkPath ?? string.Empty,
            Exists = exists,
            Blur = blur,
            ShouldPause = false,
            PlaybackState = VideoPlaybackState.Disabled,
            FallbackAssetPath = null,
            DetectedMimeType = exists && artworkPath != null ? GetImageMimeType(artworkPath) : null
        };
    }

    private static string? ResolvePath(string? rawAsset, string? baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(rawAsset)) return null;
        if (Path.IsPathRooted(rawAsset)) return rawAsset;
        if (!string.IsNullOrWhiteSpace(baseDirectory)) return Path.Combine(baseDirectory, rawAsset);
        return rawAsset;
    }

    private static string GetImageMimeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            _ => "image/jpeg"
        };
    }
}
