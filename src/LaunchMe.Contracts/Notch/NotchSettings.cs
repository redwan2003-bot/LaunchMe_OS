using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Notch;

public sealed class NotchSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("bg")]
    public string Bg { get; set; } = "BlackGlass";

    [JsonPropertyName("openMode")]
    public string OpenMode { get; set; } = "Hover";

    [JsonPropertyName("hideInGames")]
    public bool HideInGames { get; set; } = true;

    [JsonPropertyName("excludeCapture")]
    public bool ExcludeCapture { get; set; } = true;

    [JsonPropertyName("widgets")]
    public List<string> Widgets { get; set; } = ["Music:Compact", "Timer:Compact"];
}
