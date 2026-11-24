using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LaunchMe.Core.Apps;

public sealed class IconCacheService
{
    private readonly string _cacheFolder;

    public IconCacheService(string? cacheFolder = null)
    {
        _cacheFolder = cacheFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LaunchMe", "IconCache");

        Directory.CreateDirectory(_cacheFolder);
    }

    public string GetOrExtractIcon(string appId, string executablePath, int targetSize = 128)
    {
        var sanitizedAppId = string.Join("_", appId.Split(Path.GetInvalidFileNameChars()));
        var cacheFilePath = Path.Combine(_cacheFolder, $"{sanitizedAppId}_{targetSize}.png");

        if (File.Exists(cacheFilePath))
        {
            return cacheFilePath;
        }

        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return CreatePlaceholderIcon(cacheFilePath, targetSize);
        }

        try
        {
            using var sysIcon = Icon.ExtractAssociatedIcon(executablePath);
            if (sysIcon == null)
            {
                return CreatePlaceholderIcon(cacheFilePath, targetSize);
            }

            using var rawBitmap = sysIcon.ToBitmap();
            using var resizedBitmap = new Bitmap(targetSize, targetSize, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(resizedBitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                g.DrawImage(rawBitmap, new Rectangle(0, 0, targetSize, targetSize));
            }

            resizedBitmap.Save(cacheFilePath, ImageFormat.Png);
            return cacheFilePath;
        }
        catch
        {
            return CreatePlaceholderIcon(cacheFilePath, targetSize);
        }
    }

    public string ConvertIconToBase64(string iconFilePath)
    {
        if (!File.Exists(iconFilePath)) return string.Empty;
        var bytes = File.ReadAllBytes(iconFilePath);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    private string CreatePlaceholderIcon(string cacheFilePath, int size)
    {
        if (File.Exists(cacheFilePath)) return cacheFilePath;

        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(Color.FromArgb(220, 30, 64, 175));
            g.FillEllipse(brush, 4, 4, size - 8, size - 8);

            using var pen = new Pen(Color.White, 3f);
            g.DrawEllipse(pen, 4, 4, size - 8, size - 8);
        }

        bmp.Save(cacheFilePath, ImageFormat.Png);
        return cacheFilePath;
    }
}
