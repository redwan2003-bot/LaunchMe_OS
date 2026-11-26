using LaunchMe.Contracts.Clipboard;

namespace LaunchMe.UI.Services;

/// <summary>
/// Materializes Clipboard image payloads into temporary PNG files on disk so
/// WinUI DragStarting can attach real StorageItems + Bitmap payloads for
/// Finder-level drag-out directly into external Windows applications.
/// </summary>
public static class ClipboardDragPayloadHelper
{
    public static string? MaterializeImageDragFile(ClipboardEntry? entry, string? tempDirectory = null)
    {
        if (entry == null) return null;

        string? base64 = !string.IsNullOrWhiteSpace(entry.DataPayloadBase64)
            ? entry.DataPayloadBase64
            : entry.PreviewThumbnailBase64;

        if (string.IsNullOrWhiteSpace(base64)) return null;

        int commaIdx = base64.IndexOf(',');
        if (commaIdx >= 0)
        {
            base64 = base64[(commaIdx + 1)..];
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            if (bytes.Length == 0) return null;

            string dir = tempDirectory ?? Path.Combine(Path.GetTempPath(), "LaunchMeDragDrop");
            Directory.CreateDirectory(dir);
            string safeId = string.Join("_", entry.Id.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(dir, $"clipboard_{safeId}.png");
            File.WriteAllBytes(filePath, bytes);
            return filePath;
        }
        catch
        {
            return null;
        }
    }
}
