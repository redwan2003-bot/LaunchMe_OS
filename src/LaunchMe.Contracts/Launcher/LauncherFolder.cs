using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Launcher;

public sealed class LauncherFolder
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("colorHex")]
    public string? ColorHex { get; set; }

    [JsonPropertyName("emoji")]
    public string? Emoji { get; set; }

    [JsonPropertyName("iconPath")]
    public string? IconPath { get; set; }

    [JsonPropertyName("gridColumns")]
    public int GridColumns { get; set; } = 4;

    [JsonPropertyName("gridRows")]
    public int GridRows { get; set; } = 6;

    [JsonPropertyName("isEmbeddedFolder")]
    public bool IsEmbeddedFolder { get; set; }

    [JsonPropertyName("folderPath")]
    public string? FolderPath { get; set; }

    [JsonPropertyName("items")]
    public List<LauncherItem> Items { get; set; } = [];
}
