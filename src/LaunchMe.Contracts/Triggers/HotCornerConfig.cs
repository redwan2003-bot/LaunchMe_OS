using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Triggers;

public enum HotCornerAction
{
    None,
    ToggleLauncher,
    ToggleNotch,
    ToggleDock,
    ShowDesktop,
    LockScreen
}

public sealed class HotCornerConfig
{
    [JsonPropertyName("topLeft")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public HotCornerAction TopLeft { get; set; } = HotCornerAction.None;

    [JsonPropertyName("topRight")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public HotCornerAction TopRight { get; set; } = HotCornerAction.None;

    [JsonPropertyName("bottomLeft")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public HotCornerAction BottomLeft { get; set; } = HotCornerAction.None;

    [JsonPropertyName("bottomRight")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public HotCornerAction BottomRight { get; set; } = HotCornerAction.None;

    [JsonPropertyName("dwellTimeMs")]
    public int DwellTimeMs { get; set; } = 150;
}
