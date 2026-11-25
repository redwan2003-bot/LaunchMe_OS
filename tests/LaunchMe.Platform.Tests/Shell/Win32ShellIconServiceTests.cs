using System.Buffers.Binary;
using System.IO;
using FluentAssertions;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Platform.Win32.Shell;
using Xunit;

namespace LaunchMe.Platform.Tests.Shell;

public class Win32ShellIconServiceTests
{
    private static readonly byte[] ExpectedPngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void ImplementsIShellIconExtractor()
    {
        IShellIconExtractor extractor = new Win32ShellIconService();
        extractor.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"C:\LaunchMe_NonExistent_Path_12345\missing_app.exe")]
    public void ExtractHighResPng_NonexistentOrEmptyPath_ReturnsNullCleanly(string path)
    {
        var service = new Win32ShellIconService();

        byte[]? result = service.ExtractHighResPng(path);

        result.Should().BeNull();
    }

    [Fact]
    public void ExtractHighResPng_InvalidTargetSize_ReturnsNullCleanly()
    {
        var service = new Win32ShellIconService();
        string notepadPath = Path.Combine(Environment.SystemDirectory, "notepad.exe");

        service.ExtractHighResPng(notepadPath, targetSize: 0).Should().BeNull();
        service.ExtractHighResPng(notepadPath, targetSize: -32).Should().BeNull();
    }

    [Fact]
    public void ExtractHighResPng_ValidSystemExecutable_ExtractsValidRgbaPngBytes()
    {
        string notepadPath = Path.Combine(Environment.SystemDirectory, "notepad.exe");
        if (!File.Exists(notepadPath))
        {
            return;
        }

        var service = new Win32ShellIconService();
        byte[]? pngBytes = service.ExtractHighResPng(notepadPath, targetSize: 256, thumbnailIfAvailable: true);

        pngBytes.Should().NotBeNull();
        pngBytes!.Length.Should().BeGreaterThan(32);
        pngBytes[..8].Should().Equal(ExpectedPngHeader);

        // Verify IHDR chunk dimensions and RGBA color type (6).
        pngBytes.AsSpan(12, 4).SequenceEqual("IHDR"u8).Should().BeTrue();
        int width = BinaryPrimitives.ReadInt32BigEndian(pngBytes.AsSpan(16, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(pngBytes.AsSpan(20, 4));
        byte bitDepth = pngBytes[24];
        byte colorType = pngBytes[25];

        width.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(256);
        height.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(256);
        bitDepth.Should().Be(8);
        colorType.Should().Be(6);
    }

    [Fact]
    public void ExtractHighResPng_IconOnlyFlag_ExtractsValidPngBytes()
    {
        string notepadPath = Path.Combine(Environment.SystemDirectory, "notepad.exe");
        if (!File.Exists(notepadPath))
        {
            return;
        }

        var service = new Win32ShellIconService();
        byte[]? pngBytes = service.ExtractHighResPng(notepadPath, targetSize: 64, thumbnailIfAvailable: false);

        pngBytes.Should().NotBeNull();
        pngBytes![..8].Should().Equal(ExpectedPngHeader);
    }
}
