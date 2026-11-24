using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Launcher;

public enum LauncherItemType
{
    App,
    Folder,
    EmbeddedFolder,
    Workflow,
    Widget
}

public sealed class LauncherItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("itemType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LauncherItemType ItemType { get; set; } = LauncherItemType.App;

    [JsonPropertyName("gridRow")]
    public int GridRow { get; set; }

    [JsonPropertyName("gridColumn")]
    public int GridColumn { get; set; }

    [JsonPropertyName("appId")]
    public string? AppId { get; set; }

    [JsonPropertyName("targetPayload")]
    public string? TargetPayload { get; set; }

    [JsonPropertyName("customIconPath")]
    public string? CustomIconPath { get; set; }

    [JsonPropertyName("folderId")]
    public string? FolderId { get; set; }
}
