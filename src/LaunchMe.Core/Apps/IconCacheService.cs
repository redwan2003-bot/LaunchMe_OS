using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Xml.Linq;
using LaunchMe.Contracts.Indexing;

namespace LaunchMe.Core.Apps;

public sealed class IconCacheService
{
    private const int LegacyIconThresholdPx = 96;
    private readonly string _cacheFolder;
    private readonly IShellIconExtractor? _shellIconExtractor;

    public IconCacheService(string? cacheFolder = null, IShellIconExtractor? shellIconExtractor = null)
    {
        _cacheFolder = cacheFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LaunchMe", "IconCache");
        _shellIconExtractor = shellIconExtractor;

        Directory.CreateDirectory(_cacheFolder);
    }

    public string GetOrExtractIcon(string appId, string executablePath, int targetSize = 128)
    {
        var normalizedSize = Math.Max(16, targetSize);
        var sanitizedAppId = string.Join("_", appId.Split(Path.GetInvalidFileNameChars()));
        string suffix = normalizedSize >= 256 ? $"{normalizedSize}_g2" : $"{normalizedSize}";
        var cacheFilePath = Path.Combine(_cacheFolder, $"{sanitizedAppId}_{suffix}.png");

        if (File.Exists(cacheFilePath))
        {
            return cacheFilePath;
        }

        var fileExists = !string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath);
        var dirExists = !string.IsNullOrWhiteSpace(executablePath) && Directory.Exists(executablePath);

        if (!fileExists && !dirExists)
        {
            return CreatePlaceholderIcon(cacheFilePath, normalizedSize);
        }

        // 1. Prioritize MSIX high-res Assets (scale-400 / scale-200 PNGs)
        var msixAssetPath = TryFindMsixHighResAsset(executablePath);
        if (!string.IsNullOrWhiteSpace(msixAssetPath) && File.Exists(msixAssetPath))
        {
            try
            {
                using var msixBitmap = new Bitmap(msixAssetPath);
                SaveProcessedIcon(msixBitmap, cacheFilePath, normalizedSize);
                return cacheFilePath;
            }
            catch
            {
                // Fall through to Shell / Icon extraction if MSIX asset is unreadable
            }
        }

        // 2. Try high-res Windows Shell extractor (256px for default 128px; up to 1024px for >= 256px)
        if (_shellIconExtractor is not null && fileExists)
        {
            try
            {
                int shellRequestPx = normalizedSize <= 128 ? 256 : Math.Clamp(normalizedSize * 2, 512, 1024);
                var highResPng = _shellIconExtractor.ExtractHighResPng(executablePath, shellRequestPx);
                if (highResPng is { Length: > 0 })
                {
                    using var stream = new MemoryStream(highResPng);
                    using var shellBitmap = new Bitmap(stream);
                    SaveProcessedIcon(shellBitmap, cacheFilePath, normalizedSize);
                    return cacheFilePath;
                }
            }
            catch
            {
                // Fall through to legacy Win32 icon extraction
            }
        }

        // 3. Fallback to legacy Icon.ExtractAssociatedIcon
        if (fileExists)
        {
            try
            {
                using var sysIcon = Icon.ExtractAssociatedIcon(executablePath);
                if (sysIcon is not null)
                {
                    using var rawBitmap = sysIcon.ToBitmap();
                    SaveProcessedIcon(rawBitmap, cacheFilePath, normalizedSize);
                    return cacheFilePath;
                }
            }
            catch
            {
                return CreatePlaceholderIcon(cacheFilePath, normalizedSize);
            }
        }

        return CreatePlaceholderIcon(cacheFilePath, normalizedSize);
    }

    /// <summary>
    /// Searches sibling AppxManifest.xml and Assets directories for MSIX scale-400 / scale-200 PNG assets.
    /// </summary>
    public string? TryFindMsixHighResAsset(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        string? startDirectory;
        try
        {
            startDirectory = Directory.Exists(executablePath)
                ? Path.GetFullPath(executablePath)
                : Path.GetDirectoryName(Path.GetFullPath(executablePath));
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(startDirectory) || !Directory.Exists(startDirectory))
        {
            return null;
        }

        // Walk up at most 2 parent directories to locate the package root (AppxManifest.xml or Assets)
        var candidateDirs = new List<string> { startDirectory };
        var current = Directory.GetParent(startDirectory);
        for (var i = 0; i < 2 && current is not null; i++)
        {
            candidateDirs.Add(current.FullName);
            current = current.Parent;
        }

        foreach (var dir in candidateDirs)
        {
            var manifestPath = Path.Combine(dir, "AppxManifest.xml");
            if (File.Exists(manifestPath))
            {
                var fromManifest = ResolveAssetFromManifest(manifestPath, dir);
                if (fromManifest is not null)
                {
                    return fromManifest;
                }
            }

            var assetsDir = Path.Combine(dir, "Assets");
            if (Directory.Exists(assetsDir))
            {
                var fromAssets = FindBestAssetInFolder(assetsDir);
                if (fromAssets is not null)
                {
                    return fromAssets;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Mounts a legacy (&lt; 96px) icon inside a uniform G2 continuous Squircle glass pedestal
    /// tinted with the icon's sampled dominant edge color.
    /// </summary>
    public static Bitmap NormalizeToLuxurySquircle(Bitmap source, int targetSize = 128)
    {
        ArgumentNullException.ThrowIfNull(source);
        var size = Math.Max(16, targetSize);

        var edgeTint = SampleDominantEdgeTint(source);
        var canvas = new Bitmap(size, size, PixelFormat.Format32bppArgb);

        using var g = Graphics.FromImage(canvas);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.Clear(Color.Transparent);

        var margin = Math.Max(2f, size * 0.04f);
        var rect = new RectangleF(margin, margin, size - (margin * 2f), size - (margin * 2f));
        using var squirclePath = CreateG2SquirclePath(rect, exponent: 4.5, segments: 128);

        var topTint = BlendColor(edgeTint, Color.White, 0.28f);
        var bottomTint = BlendColor(edgeTint, Color.FromArgb(255, 12, 16, 28), 0.34f);

        using (var bgBrush = new LinearGradientBrush(
            new PointF(0f, rect.Top),
            new PointF(0f, rect.Bottom),
            Color.FromArgb(232, topTint.R, topTint.G, topTint.B),
            Color.FromArgb(242, bottomTint.R, bottomTint.G, bottomTint.B)))
        {
            g.FillPath(bgBrush, squirclePath);
        }

        // Top-half frosted glass specular sheen clipped inside the squircle
        var prevClip = g.Clip;
        g.SetClip(squirclePath);
        var sheenRect = new RectangleF(rect.Left, rect.Top, rect.Width, rect.Height * 0.55f);
        using (var sheenBrush = new LinearGradientBrush(
            new PointF(0f, sheenRect.Top),
            new PointF(0f, sheenRect.Bottom),
            Color.FromArgb(72, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255)))
        {
            g.FillRectangle(sheenBrush, sheenRect);
        }

        // Mount the legacy icon centered inside the glass pedestal (~58% of tile width)
        var insetSize = Math.Max(8f, rect.Width * 0.58f);
        var iconLeft = (size - insetSize) * 0.5f;
        var iconTop = (size - insetSize) * 0.5f;
        g.DrawImage(source, new RectangleF(iconLeft, iconTop, insetSize, insetSize));
        g.Clip = prevClip;

        // Subtle luminous rim highlight on the G2 squircle border
        using var rimPen = new Pen(Color.FromArgb(115, 255, 255, 255), Math.Max(1f, size * 0.015f));
        g.DrawPath(rimPen, squirclePath);

        return canvas;
    }

    public string ConvertIconToBase64(string iconFilePath)
    {
        if (!File.Exists(iconFilePath)) return string.Empty;
        var bytes = File.ReadAllBytes(iconFilePath);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    private static void SaveProcessedIcon(Bitmap rawBitmap, string cacheFilePath, int targetSize)
    {
        if (rawBitmap.Width < LegacyIconThresholdPx || rawBitmap.Height < LegacyIconThresholdPx)
        {
            using var squircleBitmap = NormalizeToLuxurySquircle(rawBitmap, targetSize);
            squircleBitmap.Save(cacheFilePath, ImageFormat.Png);
            return;
        }

        using var resizedBitmap = new Bitmap(targetSize, targetSize, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(resizedBitmap))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            // Apply macOS .icns-style uniform G2 squircle mask for >= 256px Gold-Standard icons
            // so square UWP/MSIX tiles never render with sharp 90-degree corners.
            if (targetSize >= 256)
            {
                var margin = Math.Max(1f, targetSize * 0.03f);
                var rect = new RectangleF(margin, margin, targetSize - (margin * 2f), targetSize - (margin * 2f));
                using var squirclePath = CreateG2SquirclePath(rect, exponent: 4.5, segments: 128);
                g.SetClip(squirclePath);
                g.DrawImage(rawBitmap, rect);
            }
            else
            {
                g.DrawImage(rawBitmap, new Rectangle(0, 0, targetSize, targetSize));
            }
        }

        resizedBitmap.Save(cacheFilePath, ImageFormat.Png);
    }

    /// <summary>
    /// Windows equivalent of macOS QLThumbnail: extracts a cached high-resolution Explorer
    /// preview thumbnail (PNG) for documents, images, videos, and folders.
    /// </summary>
    public string? GetOrExtractFileThumbnail(string filePath, int targetSize = 256)
    {
        if (string.IsNullOrWhiteSpace(filePath) || _shellIconExtractor is null)
        {
            return null;
        }

        if (!File.Exists(filePath) && !Directory.Exists(filePath))
        {
            return null;
        }

        string thumbsDir = Path.Combine(_cacheFolder, "thumbs");
        Directory.CreateDirectory(thumbsDir);
        string key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(filePath)))[..16];
        string thumbPath = Path.Combine(thumbsDir, $"{key}_{targetSize}.png");
        if (File.Exists(thumbPath))
        {
            return thumbPath;
        }

        try
        {
            var png = _shellIconExtractor.ExtractHighResPng(filePath, targetSize, thumbnailIfAvailable: true);
            if (png is { Length: > 0 })
            {
                File.WriteAllBytes(thumbPath, png);
                return thumbPath;
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? ResolveAssetFromManifest(string manifestPath, string packageRoot)
    {
        try
        {
            var doc = XDocument.Load(manifestPath);
            var relativeCandidates = new List<string>();

            var logoAttrs = new[] { "Square150x150Logo", "Square44x44Logo", "Square310x310Logo", "Logo" };
            foreach (var element in doc.Descendants())
            {
                foreach (var attrName in logoAttrs)
                {
                    var attr = element.Attribute(attrName);
                    if (attr is not null && !string.IsNullOrWhiteSpace(attr.Value))
                    {
                        relativeCandidates.Add(attr.Value.Trim());
                    }
                }

                if (element.Name.LocalName.Equals("Logo", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(element.Value))
                {
                    relativeCandidates.Add(element.Value.Trim());
                }
            }

            string? bestScale200 = null;
            string? bestFallback = null;

            foreach (var rel in relativeCandidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var normalizedRel = rel.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                var combined = Path.Combine(packageRoot, normalizedRel);
                var dir = Path.GetDirectoryName(combined);
                var stem = Path.GetFileNameWithoutExtension(combined);
                var ext = Path.GetExtension(combined);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(stem)) continue;

                // Prioritize scale-400 first, then targetsize-256, then scale-200
                var scale400 = Path.Combine(dir, $"{stem}.scale-400{ext}");
                if (File.Exists(scale400)) return scale400;

                var target256 = Path.Combine(dir, $"{stem}.targetsize-256{ext}");
                if (File.Exists(target256)) return target256;

                var target256Alt = Path.Combine(dir, $"{stem}.targetsize-256_altform-unplated{ext}");
                if (File.Exists(target256Alt)) return target256Alt;

                var scale200 = Path.Combine(dir, $"{stem}.scale-200{ext}");
                if (bestScale200 is null && File.Exists(scale200))
                {
                    bestScale200 = scale200;
                }

                if (bestFallback is null && File.Exists(combined))
                {
                    bestFallback = combined;
                }
            }

            if (bestScale200 is not null) return bestScale200;

            var assetsDir = Path.Combine(packageRoot, "Assets");
            if (Directory.Exists(assetsDir))
            {
                var bestFromAssets = FindBestAssetInFolder(assetsDir);
                if (bestFromAssets is not null) return bestFromAssets;
            }

            return bestFallback;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindBestAssetInFolder(string assetsDir)
    {
        try
        {
            var pngs = Directory.EnumerateFiles(assetsDir, "*.png", SearchOption.AllDirectories).ToList();
            if (pngs.Count == 0) return null;

            return pngs
                .Select(path => new { Path = path, Score = ScoreMsixAssetPath(path) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Path)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static int ScoreMsixAssetPath(string path)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();
        if (name.Contains("splashscreen") || name.Contains("wide310x150"))
        {
            return 0;
        }

        var score = 0;
        if (name.Contains("scale-400")) score += 400;
        else if (name.Contains("targetsize-256")) score += 350;
        else if (name.Contains("scale-200")) score += 200;
        else return 0;

        if (name.Contains("square150x150")) score += 50;
        else if (name.Contains("applist") || name.Contains("square44x44")) score += 40;
        else if (name.Contains("square310x310")) score += 35;
        else if (name.Contains("logo") || name.Contains("icon")) score += 25;

        if (name.Contains("altform-unplated")) score += 10;

        return score;
    }

    private static Color SampleDominantEdgeTint(Bitmap source)
    {
        var width = source.Width;
        var height = source.Height;
        double sumR = 0;
        double sumG = 0;
        double sumB = 0;
        double weightSum = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var px = source.GetPixel(x, y);
                if (px.A < 24) continue;

                var isEdgeBand = x <= 3 || x >= width - 4 || y <= 3 || y >= height - 4;
                var max = Math.Max(px.R, Math.Max(px.G, px.B));
                var min = Math.Min(px.R, Math.Min(px.G, px.B));
                var chromaWeight = 1.0 + ((max - min) / 255.0) * 2.5;
                var edgeWeight = isEdgeBand ? 2.5 : 1.0;
                var weight = (px.A / 255.0) * chromaWeight * edgeWeight;

                sumR += px.R * weight;
                sumG += px.G * weight;
                sumB += px.B * weight;
                weightSum += weight;
            }
        }

        if (weightSum <= 0.001)
        {
            return Color.FromArgb(255, 64, 96, 168);
        }

        return Color.FromArgb(
            255,
            Math.Clamp((int)Math.Round(sumR / weightSum), 0, 255),
            Math.Clamp((int)Math.Round(sumG / weightSum), 0, 255),
            Math.Clamp((int)Math.Round(sumB / weightSum), 0, 255));
    }

    private static Color BlendColor(Color a, Color b, float t)
    {
        var clamped = Math.Clamp(t, 0f, 1f);
        var r = (int)Math.Round(a.R + ((b.R - a.R) * clamped));
        var g = (int)Math.Round(a.G + ((b.G - a.G) * clamped));
        var bl = (int)Math.Round(a.B + ((b.B - a.B) * clamped));
        return Color.FromArgb(255, Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(bl, 0, 255));
    }

    private static GraphicsPath CreateG2SquirclePath(RectangleF bounds, double exponent, int segments)
    {
        var cx = bounds.Left + (bounds.Width * 0.5);
        var cy = bounds.Top + (bounds.Height * 0.5);
        var rx = bounds.Width * 0.5;
        var ry = bounds.Height * 0.5;
        var power = 2.0 / exponent;

        var points = new PointF[segments];
        for (var i = 0; i < segments; i++)
        {
            var theta = (2.0 * Math.PI * i) / segments;
            var cosT = Math.Cos(theta);
            var sinT = Math.Sin(theta);

            var x = cx + (rx * Math.Sign(cosT) * Math.Pow(Math.Abs(cosT), power));
            var y = cy + (ry * Math.Sign(sinT) * Math.Pow(Math.Abs(sinT), power));
            points[i] = new PointF((float)x, (float)y);
        }

        var path = new GraphicsPath();
        path.AddClosedCurve(points, 0.0f);
        return path;
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
