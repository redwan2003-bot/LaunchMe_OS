using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Dock;

public sealed class DockSettings
{
    [JsonPropertyName("perDisplay")]
    public bool PerDisplay { get; set; } = true;

    [JsonPropertyName("floating")]
    public bool Floating { get; set; } = false;

    [JsonPropertyName("maxWindowMode")]
    public string MaxWindowMode { get; set; } = "ReserveWorkArea";

    [JsonPropertyName("widgets")]
    public List<string> Widgets { get; set; } = ["SystemStats", "MusicCompact", "ToolBar"];
}
