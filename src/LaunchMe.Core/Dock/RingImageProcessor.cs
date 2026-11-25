using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using LaunchMe.Contracts.Dock;

namespace LaunchMe.Core.Dock;

public sealed class RingImageProcessor : IRingImageProcessor
{
    public byte[] ProcessImageToRings(byte[] inputImageBytes, RingStyleConfig config)
    {
        if (inputImageBytes == null || inputImageBytes.Length == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(config);

        try
        {
            using var inStream = new MemoryStream(inputImageBytes);
            using var originalBitmap = new Bitmap(inStream);

            int width = originalBitmap.Width;
            int height = originalBitmap.Height;
            if (width <= 0 || height <= 0)
            {
                return [];
            }

            using var outputBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(outputBitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Draw base image adjusted for brightness and saturation
                var colorMatrix = CreateColorMatrix(config.Brightness, config.Saturation);
                using var imageAttr = new ImageAttributes();
                imageAttr.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                g.DrawImage(
                    originalBitmap,
                    new Rectangle(0, 0, width, height),
                    0, 0, width, height,
                    GraphicsUnit.Pixel,
                    imageAttr);

                // Overlay concentric ring masks
                float centerX = width / 2.0f;
                float centerY = height / 2.0f;
                float maxRadius = Math.Min(centerX, centerY);

                int ringCount = Math.Max(1, config.RingCount);
                float step = maxRadius / ringCount;
                float thickness = (float)Math.Max(1.0, config.RingThickness);

                using var ringPen = new Pen(Color.FromArgb(90, 255, 255, 255), thickness);
                for (int i = 1; i <= ringCount; i++)
                {
                    float radius = i * step;
                    float diameter = radius * 2.0f;
                    g.DrawEllipse(ringPen, centerX - radius, centerY - radius, diameter, diameter);
                }
            }

            using var outStream = new MemoryStream();
            outputBitmap.Save(outStream, ImageFormat.Png);
            return outStream.ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static ColorMatrix CreateColorMatrix(double brightness, double saturation)
    {
        float b = (float)Math.Clamp(brightness, 0.0, 3.0);
        float s = (float)Math.Clamp(saturation, 0.0, 3.0);

        // Standard luminance weights: 0.3086, 0.6094, 0.0820
        float rWeight = 0.3086f * (1.0f - s);
        float gWeight = 0.6094f * (1.0f - s);
        float bWeight = 0.0820f * (1.0f - s);

        return new ColorMatrix(
        [
            [ (rWeight + s) * b, gWeight * b,       bWeight * b,       0, 0 ],
            [ rWeight * b,       (gWeight + s) * b, bWeight * b,       0, 0 ],
            [ rWeight * b,       gWeight * b,       (bWeight + s) * b, 0, 0 ],
            [ 0,                 0,                 0,                 1, 0 ],
            [ 0,                 0,                 0,                 0, 1 ]
        ]);
    }
}
