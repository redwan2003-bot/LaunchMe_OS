using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Spaces;

public enum WallpaperType
{
    Static,
    Dynamic,
    LiveVideo,
    MusicArt
}

public sealed class WallpaperSettings
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WallpaperType Type { get; set; } = WallpaperType.Static;

    [JsonPropertyName("asset")]
    public string Asset { get; set; } = string.Empty;

    [JsonPropertyName("blur")]
    public double Blur { get; set; } = 0.0;

    [JsonPropertyName("pauseOnHide")]
    public bool PauseOnHide { get; set; } = true;
}
