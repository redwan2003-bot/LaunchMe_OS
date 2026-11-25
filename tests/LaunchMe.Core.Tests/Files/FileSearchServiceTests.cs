using FluentAssertions;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Core.Files;
using Moq;
using Xunit;

namespace LaunchMe.Core.Tests.Files;

public sealed class FileSearchServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSearchService _service = new();

    public FileSearchServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"launchme_fs_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "Budget_2026.xlsx"), "budget");
        File.WriteAllText(Path.Combine(_tempDir, "Notes.txt"), "notes");
    }

    [Fact]
    public async Task SearchFiles_ScopedToGrantedDirectoriesOnly()
    {
        // Unapproved: empty results
        var unapprovedResults = await _service.SearchFilesAsync("Budget");
        unapprovedResults.Should().BeEmpty();

        // Grant scope: yields results
        _service.GrantFolderScope(_tempDir);
        var approvedResults = await _service.SearchFilesAsync("Budget");
        approvedResults.Should().HaveCount(1);
        approvedResults[0].FileName.Should().Be("Budget_2026.xlsx");

        // Revoke scope: empty again
        _service.RevokeFolderScope(_tempDir);
        var revokedResults = await _service.SearchFilesAsync("Budget");
        revokedResults.Should().BeEmpty();
    }

    [Fact]
    public async Task CopyFileToClipboardAsync_ExistingFile_InvokesClipboardWriterWithFullPath()
    {
        var writerMock = new Mock<IClipboardWriter>();
        writerMock.Setup(w => w.SetFiles(It.IsAny<IEnumerable<string>>())).Returns(true);
        var service = new FileSearchService(writerMock.Object);
        var filePath = Path.Combine(_tempDir, "Notes.txt");

        await service.CopyFileToClipboardAsync(filePath);

        writerMock.Verify(
            w => w.SetFiles(It.Is<IEnumerable<string>>(paths => paths.SequenceEqual(new[] { Path.GetFullPath(filePath) }))),
            Times.Once);
    }

    [Fact]
    public async Task CopyFileToClipboardAsync_ExistingDirectory_InvokesClipboardWriter()
    {
        var writerMock = new Mock<IClipboardWriter>();
        writerMock.Setup(w => w.SetFiles(It.IsAny<IEnumerable<string>>())).Returns(true);
        var service = new FileSearchService(writerMock.Object);

        await service.CopyFileToClipboardAsync(_tempDir);

        writerMock.Verify(
            w => w.SetFiles(It.Is<IEnumerable<string>>(paths => paths.SequenceEqual(new[] { Path.GetFullPath(_tempDir) }))),
            Times.Once);
    }

    [Fact]
    public async Task CopyFileToClipboardAsync_MissingFile_DoesNotInvokeClipboardWriter()
    {
        var writerMock = new Mock<IClipboardWriter>();
        var service = new FileSearchService(writerMock.Object);
        var missingPath = Path.Combine(_tempDir, "Missing.txt");

        await service.CopyFileToClipboardAsync(missingPath);

        writerMock.Verify(w => w.SetFiles(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task CopyFileToClipboardAsync_WithoutClipboardWriter_CompletesWithoutThrowing()
    {
        var filePath = Path.Combine(_tempDir, "Notes.txt");
        var act = async () => await _service.CopyFileToClipboardAsync(filePath);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CopyFileToClipboardAsync_EmptyPath_ThrowsArgumentException()
    {
        var act = async () => await _service.CopyFileToClipboardAsync("  ");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
