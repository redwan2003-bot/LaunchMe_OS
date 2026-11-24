namespace LaunchMe.Core.Wallpapers;

/// <summary>
/// Resolves and validates video wallpaper files and implements the Pause-on-Hide 0% idle GPU rule.
/// </summary>
public static class VideoAssetResolver
{
    public static readonly IReadOnlyList<string> SupportedExtensions =
    [
        ".mp4",
        ".mov",
        ".avi",
        ".mkv",
        ".webm"
    ];

    public static bool IsSupportedVideo(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var ext = Path.GetExtension(filePath);
        return SupportedExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public static string DetectMimeType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Resolves an asset path, checking absolute paths, base directory relative paths, or standard assets.
    /// </summary>
    public static string? ResolveVideoPath(string? rawAsset, string? baseDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(rawAsset))
        {
            return null;
        }

        // Check if path is absolute and exists
        if (Path.IsPathRooted(rawAsset) && File.Exists(rawAsset))
        {
            return rawAsset;
        }

        // Check relative to base directory
        if (!string.IsNullOrWhiteSpace(baseDirectory) && Directory.Exists(baseDirectory))
        {
            var candidate = Path.Combine(baseDirectory, rawAsset);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // Return path as-is if rooted (even if absent on disk, for mock/unit tests), otherwise combined
        if (Path.IsPathRooted(rawAsset))
        {
            return rawAsset;
        }

        return !string.IsNullOrWhiteSpace(baseDirectory)
            ? Path.Combine(baseDirectory, rawAsset)
            : rawAsset;
    }

    /// <summary>
    /// Implements the Pause-on-Hide GPU Rule:
    /// When the Full-Screen Launcher is hidden, video playback MUST pause immediately (0% idle GPU).
    /// </summary>
    public static VideoPlaybackState EvaluatePlaybackState(
        bool isLauncherVisible,
        bool pauseOnHide,
        bool assetExists)
    {
        if (!assetExists)
        {
            return VideoPlaybackState.Disabled;
        }

        if (pauseOnHide && !isLauncherVisible)
        {
            return VideoPlaybackState.Paused;
        }

        return isLauncherVisible ? VideoPlaybackState.Playing : VideoPlaybackState.Paused;
    }
}
