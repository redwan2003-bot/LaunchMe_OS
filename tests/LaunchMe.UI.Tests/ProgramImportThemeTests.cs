using FluentAssertions;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Core.Spaces;
using Xunit;
using UIProgram = LaunchMe.UI.Program;

namespace LaunchMe.UI.Tests;

public sealed class ProgramImportThemeTests
{
    [Fact]
    public void ExtractImportThemePath_RejectsFlagsAndNonThemeExtensions_AndNormalizesValidPaths()
    {
        UIProgram.ExtractImportThemePath([]).Should().BeNull();
        UIProgram.ExtractImportThemePath(["--import-theme", "--other-flag"]).Should().BeNull();
        UIProgram.ExtractImportThemePath(["--import-theme", @"C:\themes\bad.zip"]).Should().BeNull();
        UIProgram.ExtractImportThemePath([@"C:\themes\bad.txt"]).Should().BeNull();
        UIProgram.ExtractImportThemePath(["--help"]).Should().BeNull();

        string? extractedFlag = UIProgram.ExtractImportThemePath(["--import-theme", @".\sample.launchmetheme"]);
        extractedFlag.Should().Be(Path.GetFullPath(@".\sample.launchmetheme"));

        string? extractedSingle = UIProgram.ExtractImportThemePath([@".\direct.launchmetheme"]);
        extractedSingle.Should().Be(Path.GetFullPath(@".\direct.launchmetheme"));
    }

    [Fact]
    public async Task ImportThemePackage_HandlesValidAndCorruptArchivesGracefully()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"launchme-import-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string dbPath = Path.Combine(tempDir, "test.db");
        string validThemePath = Path.Combine(tempDir, "custom.launchmetheme");
        string corruptThemePath = Path.Combine(tempDir, "corrupt.launchmetheme");
        string wrongExtPath = Path.Combine(tempDir, "wrong.zip");

        try
        {
            // Seed a valid .launchmetheme package using a separate temp store
            string exportDbPath = Path.Combine(tempDir, "export.db");
            using (var exportStore = new SqliteSpaceStore(exportDbPath))
            {
                var customSpace = new SpaceModel
                {
                    SpaceId = "space-exported",
                    Name = "Imported Sunset",
                    Theme = new ThemeSettings { TintHex = "#EA580C" }
                };
                await exportStore.SaveSpaceAsync(customSpace);
                await exportStore.ExportThemeAsync("space-exported", validThemePath);
            }

            // Valid archive succeeds and switches active space
            UIProgram.ImportThemePackage(validThemePath, dbPath).Should().BeTrue();
            using (var verifyStore = new SqliteSpaceStore(dbPath))
            {
                var active = await verifyStore.GetActiveSpaceAsync();
                active.Should().NotBeNull();
                active!.Name.Should().Be("Imported Sunset");
            }

            // Corrupt archive fails gracefully without throwing
            await File.WriteAllTextAsync(corruptThemePath, "not-a-valid-zip-archive");
            UIProgram.ImportThemePackage(corruptThemePath, dbPath).Should().BeFalse();

            // Missing or wrong extension fails gracefully without throwing
            await File.WriteAllTextAsync(wrongExtPath, "zip");
            UIProgram.ImportThemePackage(wrongExtPath, dbPath).Should().BeFalse();
            UIProgram.ImportThemePackage(Path.Combine(tempDir, "missing.launchmetheme"), dbPath).Should().BeFalse();
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch
            {
                // Best-effort temp cleanup when SQLite connection pool retains handles briefly on Windows
            }
        }
    }
}
