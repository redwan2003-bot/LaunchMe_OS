using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Launcher;

public sealed class LauncherSettings
{
    [JsonPropertyName("classicView")]
    public bool ClassicView { get; set; } = true;

    [JsonPropertyName("freeLayout")]
    public bool FreeLayout { get; set; } = false;

    [JsonPropertyName("hideTitles")]
    public bool HideTitles { get; set; } = false;

    [JsonPropertyName("iconScale")]
    public double IconScale { get; set; } = 1.0;

    [JsonPropertyName("folderGrid")]
    public string FolderGrid { get; set; } = "4x6";

    [JsonPropertyName("pages")]
    public List<LauncherPage> Pages { get; set; } = [];

    [JsonPropertyName("folders")]
    public List<LauncherFolder> Folders { get; set; } = [];
}
