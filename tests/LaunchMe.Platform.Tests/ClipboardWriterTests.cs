using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using LaunchMe.Platform.Win32.Clipboard;
using LaunchMe.Platform.Win32.Native;
using Xunit;

namespace LaunchMe.Platform.Tests;

public class ClipboardWriterTests
{
    [Fact]
    public void SetText_NullInput_ReturnsFalse()
    {
        var writer = new Win32ClipboardWriter();

        bool result = writer.SetText(null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void SetText_ValidText_WritesSuccessfully()
    {
        var writer = new Win32ClipboardWriter();

        bool result = writer.SetText("LaunchMe-Win Clipboard Text");

        result.Should().BeTrue();
    }

    [Fact]
    public void SetText_EmptyString_WritesSuccessfully()
    {
        var writer = new Win32ClipboardWriter();

        bool result = writer.SetText(string.Empty);

        result.Should().BeTrue();
    }

    [Fact]
    public void SetFiles_NullOrEmptyOrNonExistent_ReturnsFalse()
    {
        var writer = new Win32ClipboardWriter();

        writer.SetFiles(null!).Should().BeFalse();
        writer.SetFiles([]).Should().BeFalse();
        writer.SetFiles(["", "   ", @"Z:\nonexistent\launchme_missing_file_12345.txt"]).Should().BeFalse();
    }

    [Fact]
    public void SetFiles_InvalidPathCharacters_ReturnsFalseWithoutThrowing()
    {
        var writer = new Win32ClipboardWriter();

        bool result = writer.SetFiles(["\0invalid\0path"]);

        result.Should().BeFalse();
    }

    [Fact]
    public void SetFiles_ExistingFile_WritesCfHdropSuccessfully()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"launchme_clip_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            var writer = new Win32ClipboardWriter();

            bool result = writer.SetFiles([tempFile, @"Z:\missing\file.txt"]);

            result.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void BuildDropFilesPayload_NullOrEmpty_ThrowsArgumentException()
    {
        Action actNull = () => Win32ClipboardWriter.BuildDropFilesPayload(null!);
        Action actEmpty = () => Win32ClipboardWriter.BuildDropFilesPayload([]);

        actNull.Should().Throw<ArgumentNullException>();
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildDropFilesPayload_MultiplePaths_SerializesDropFilesHeaderAndDoubleNullTerminatedUtf16()
    {
        string[] paths = [@"C:\Folder\alpha.txt", @"D:\Work\beta.png"];

        byte[] payload = Win32ClipboardWriter.BuildDropFilesPayload(paths);

        int headerSize = Marshal.SizeOf<Win32Interop.DROPFILES>();
        headerSize.Should().Be(20);

        var header = MemoryMarshal.Read<Win32Interop.DROPFILES>(payload.AsSpan(0, headerSize));
        header.pFiles.Should().Be(20u);
        header.pt.x.Should().Be(0);
        header.pt.y.Should().Be(0);
        header.fNC.Should().Be(0);
        header.fWide.Should().Be(1);

        int expectedBodyBytes =
            Encoding.Unicode.GetByteCount(paths[0]) + 2 +
            Encoding.Unicode.GetByteCount(paths[1]) + 2 +
            2;
        payload.Length.Should().Be(20 + expectedBodyBytes);

        // Verify double-null termination (4 zero bytes at the end).
        payload[^4..].Should().Equal(0, 0, 0, 0);

        string decoded = Encoding.Unicode.GetString(payload, headerSize, payload.Length - headerSize);
        decoded.Should().Be($"{paths[0]}\0{paths[1]}\0\0");
    }

    [Fact]
    public void Win32Interop_StructLayouts_HaveExpectedSizes()
    {
        Marshal.SizeOf<Win32Interop.DROPFILES>().Should().Be(20);
        Marshal.SizeOf<Win32Interop.NOTIFYICONDATAW>().Should().Be(976);
    }
}
