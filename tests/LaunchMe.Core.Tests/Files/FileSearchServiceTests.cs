using FluentAssertions;
using LaunchMe.Core.Files;
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

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
