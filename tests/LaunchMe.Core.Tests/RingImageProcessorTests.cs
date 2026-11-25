using System.Drawing;
using System.Drawing.Imaging;
using LaunchMe.Contracts.Dock;
using LaunchMe.Core.Dock;
using Xunit;

namespace LaunchMe.Core.Tests;

public sealed class RingImageProcessorTests
{
    [Fact]
    public void ProcessImageToRings_EmptyOrNullInput_ReturnsEmpty()
    {
        var processor = new RingImageProcessor();
        var config = new RingStyleConfig();

        var resultEmpty = processor.ProcessImageToRings([], config);
        Assert.Empty(resultEmpty);
    }

    [Fact]
    public void ProcessImageToRings_ValidImage_ProducesStyledPngOutput()
    {
        var processor = new RingImageProcessor();
        var config = new RingStyleConfig(RingCount: 8, RingThickness: 4.0, Spacing: 6.0, Brightness: 1.2, Saturation: 1.1);

        // Generate synthetic test PNG bitmap
        byte[] inputPng;
        using (var bmp = new Bitmap(100, 100, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.CornflowerBlue);
            }
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            inputPng = ms.ToArray();
        }

        var output = processor.ProcessImageToRings(inputPng, config);

        Assert.NotEmpty(output);
        using var outBmp = new Bitmap(new MemoryStream(output));
        Assert.Equal(100, outBmp.Width);
        Assert.Equal(100, outBmp.Height);
    }
}
