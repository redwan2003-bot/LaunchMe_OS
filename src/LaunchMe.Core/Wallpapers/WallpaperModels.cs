using LaunchMe.Contracts.Spaces;

namespace LaunchMe.Core.Wallpapers;

public enum SunPhase
{
    AstronomicalNight,
    NauticalTwilight,
    CivilTwilight,
    Dawn,
    Sunrise,
    Morning,
    SolarNoon,
    Afternoon,
    GoldenHour,
    Sunset,
    Dusk
}

public enum VideoPlaybackState
{
    Playing,
    Paused,
    Disabled
}

public sealed record SolarInfo
{
    public required DateTimeOffset Timestamp { get; init; }
    public required double ElevationDegrees { get; init; }
    public required double AzimuthDegrees { get; init; }
    public required bool IsDaylight { get; init; }
    public required SunPhase Phase { get; init; }
    public required TimeSpan SunriseTime { get; init; }
    public required TimeSpan SunsetTime { get; init; }
    public required TimeSpan SolarNoonTime { get; init; }
}

public sealed record ResolvedWallpaper
{
    public required WallpaperType Type { get; init; }
    public required string ResolvedAssetPath { get; init; }
    public required bool Exists { get; init; }
    public required double Blur { get; init; }
    public required bool ShouldPause { get; init; }
    public required VideoPlaybackState PlaybackState { get; init; }
    public string? FallbackAssetPath { get; init; }
    public string? DetectedMimeType { get; init; }
}
