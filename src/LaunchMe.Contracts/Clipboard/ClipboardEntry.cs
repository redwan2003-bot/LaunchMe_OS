using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Clipboard;

public enum ClipboardCategory
{
    Recent,
    Images,
    Colors,
    Text,
    Files,
    Code,
    Screenshots,
    Favorites
}

public sealed class ClipboardEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("category")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClipboardCategory Category { get; set; } = ClipboardCategory.Recent;

    [JsonPropertyName("capturedAtUtc")]
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("textContent")]
    public string? TextContent { get; set; }

    [JsonPropertyName("dataPayloadBase64")]
    public string? DataPayloadBase64 { get; set; }

    [JsonPropertyName("previewThumbnailBase64")]
    public string? PreviewThumbnailBase64 { get; set; }

    [JsonPropertyName("filePaths")]
    public List<string> FilePaths { get; set; } = [];

    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; set; }

    [JsonPropertyName("sourceProcessName")]
    public string? SourceProcessName { get; set; }
}
