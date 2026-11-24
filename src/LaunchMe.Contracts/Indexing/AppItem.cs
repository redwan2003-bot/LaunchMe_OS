using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Indexing;

public enum AppType
{
    Win32Shortcut,
    MsixPackage,
    ExtraSource
}

public sealed class AppItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("executablePath")]
    public string ExecutablePath { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    [JsonPropertyName("appType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppType AppType { get; set; } = AppType.Win32Shortcut;

    [JsonPropertyName("iconCachePath")]
    public string? IconCachePath { get; set; }

    [JsonPropertyName("iconBase64")]
    public string? IconBase64 { get; set; }

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = [];

    [JsonPropertyName("isHidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("customDisplayName")]
    public string? CustomDisplayName { get; set; }

    [JsonPropertyName("customIconPath")]
    public string? CustomIconPath { get; set; }

    [JsonPropertyName("isRecentlyUsed")]
    public bool IsRecentlyUsed { get; set; }

    [JsonPropertyName("lastLaunchedUtc")]
    public DateTime? LastLaunchedUtc { get; set; }

    public string DisplayName => !string.IsNullOrWhiteSpace(CustomDisplayName) ? CustomDisplayName : Name;
}
