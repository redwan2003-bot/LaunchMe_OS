namespace LaunchMe.Contracts.Media;

public sealed class MediaTrackInfo
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string AlbumTitle { get; set; } = string.Empty;
    public string? ThumbnailBase64 { get; set; }
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsPlaying { get; set; }
    public string SourceAppId { get; set; } = string.Empty;
}
