using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace LaunchMe.UI.Services;

/// <summary>
/// Captures live WinUI 3 surfaces at the current DPI scaling using
/// <see cref="RenderTargetBitmap"/>, bypassing Session-0 desktop-grab
/// restrictions. Activated via the <c>--capture-showcase</c> CLI switch.
/// </summary>
public sealed class ShowcaseCaptureService : IDisposable
{
    private readonly string _outputDir;
    private bool _disposed;

    public ShowcaseCaptureService(string? outputDirectory = null)
    {
        _outputDir = outputDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "LaunchMe Showcase");
        Directory.CreateDirectory(_outputDir);
    }

    /// <summary>Returns true when the process was launched with --capture-showcase.</summary>
    public static bool IsShowcaseMode(string[] args) =>
        args.Any(a => string.Equals(a, "--capture-showcase", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Captures a named surface to a PNG file at the caller-supplied DPI label
    /// (e.g. "100", "150", "200"). Must be called on the UI thread.
    /// Returns the full path of the saved PNG or null on failure.
    /// </summary>
    public async Task<string?> CaptureAsync(UIElement surface, string surfaceName, string dpiLabel)
    {
        if (_disposed) return null;

        try
        {
            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(surface);

            var pixels = await rtb.GetPixelsAsync();
            int width  = rtb.PixelWidth;
            int height = rtb.PixelHeight;

            // Convert IBuffer → byte[]
            byte[] pixelData = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(pixels);

            string fileName = $"{surfaceName}_{dpiLabel}pct_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = Path.Combine(_outputDir, fileName);

            await using var fileStream = File.Create(filePath);
            using var memStream = new InMemoryRandomAccessStream();
            var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
                Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId,
                memStream);

            byte[] pixelBytes = pixelData;
            encoder.SetPixelData(
                Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
                (uint)width,
                (uint)height,
                96d, 96d,
                pixelBytes);

            await encoder.FlushAsync();

            // Copy in-memory stream to file
            memStream.Seek(0);
            var reader = new DataReader(memStream);
            await reader.LoadAsync((uint)memStream.Size);
            byte[] data = new byte[memStream.Size];
            reader.ReadBytes(data);
            await fileStream.WriteAsync(data, 0, data.Length);
            return filePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShowcaseCapture] Failed to capture '{surfaceName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Captures all registered surfaces at current DPI and writes a manifest JSON.
    /// </summary>
    public async Task<IReadOnlyList<string>> CaptureAllAsync(
        IEnumerable<(UIElement Surface, string Name)> surfaces,
        string dpiLabel = "100")
    {
        var results = new List<string>();
        foreach (var (surface, name) in surfaces)
        {
            var path = await CaptureAsync(surface, name, dpiLabel);
            if (path is not null) results.Add(path);
        }

        // Write manifest
        string manifestPath = Path.Combine(_outputDir, $"manifest_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        await File.WriteAllTextAsync(
            manifestPath,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                CaptureTime = DateTime.UtcNow,
                DpiLabel    = dpiLabel + "%",
                OutputDir   = _outputDir,
                Files       = results,
            },
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        return results;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
