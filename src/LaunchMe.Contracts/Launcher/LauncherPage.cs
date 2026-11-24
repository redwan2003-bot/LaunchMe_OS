using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Launcher;

public sealed class LauncherPage
{
    [JsonPropertyName("pageIndex")]
    public int PageIndex { get; set; }

    [JsonPropertyName("items")]
    public List<LauncherItem> Items { get; set; } = [];
}
