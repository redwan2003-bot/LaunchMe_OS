using System.Text.Json.Serialization;
using LaunchMe.Contracts.Dock;
using LaunchMe.Contracts.Launcher;
using LaunchMe.Contracts.Notch;
using LaunchMe.Contracts.Workflows;

namespace LaunchMe.Contracts.Spaces;

/// <summary>
/// Canonical .launchmetheme representation and SQLite persistence entity.
/// </summary>
public sealed class SpaceModel
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "3.01";

    [JsonPropertyName("spaceId")]
    public string SpaceId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Default Space";

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("schedule")]
    public SpaceSchedule Schedule { get; set; } = new();

    [JsonPropertyName("hotkey")]
    public string Hotkey { get; set; } = "Alt+Space";

    [JsonPropertyName("theme")]
    public ThemeSettings Theme { get; set; } = new();

    [JsonPropertyName("wallpaper")]
    public WallpaperSettings Wallpaper { get; set; } = new();

    [JsonPropertyName("launcher")]
    public LauncherSettings Launcher { get; set; } = new();

    [JsonPropertyName("notch")]
    public NotchSettings Notch { get; set; } = new();

    [JsonPropertyName("dock")]
    public DockSettings Dock { get; set; } = new();

    [JsonPropertyName("workflows")]
    public List<WorkflowDefinition> Workflows { get; set; } = [];
}
