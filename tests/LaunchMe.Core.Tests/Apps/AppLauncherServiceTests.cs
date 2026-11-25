using System.Diagnostics;
using FluentAssertions;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Core.Apps;
using Xunit;

namespace LaunchMe.Core.Tests.Apps;

public sealed class AppLauncherServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dummyExePath;

    public AppLauncherServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"launchme_launcher_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _dummyExePath = Path.Combine(_tempDir, "SampleApp.exe");
        File.WriteAllText(_dummyExePath, "stub");
    }

    [Fact]
    public async Task LaunchAppAsync_NullApp_ThrowsArgumentNullException()
    {
        var service = new AppLauncherService(_ => true);
        var act = async () => await service.LaunchAppAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task LaunchAppAsync_Win32Shortcut_UpdatesRecentlyUsedAndTimestamp()
    {
        ProcessStartInfo? capturedPsi = null;
        var service = new AppLauncherService(psi =>
        {
            capturedPsi = psi;
            return true;
        });

        var app = new AppItem
        {
            Id = "lnk:sample",
            Name = "SampleApp",
            ExecutablePath = _dummyExePath,
            Arguments = "--profile dev",
            AppType = AppType.Win32Shortcut,
            IsRecentlyUsed = false,
            LastLaunchedUtc = null
        };

        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = await service.LaunchAppAsync(app);
        var after = DateTime.UtcNow.AddSeconds(1);

        result.Should().BeTrue();
        capturedPsi.Should().NotBeNull();
        capturedPsi!.FileName.Should().Be(_dummyExePath);
        capturedPsi.Arguments.Should().Be("--profile dev");
        capturedPsi.UseShellExecute.Should().BeTrue();
        app.IsRecentlyUsed.Should().BeTrue();
        app.LastLaunchedUtc.Should().NotBeNull();
        app.LastLaunchedUtc!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task LaunchAppAsync_ExtraSource_WhenStarterFails_DoesNotUpdateRecentlyUsed()
    {
        var service = new AppLauncherService(_ => false);
        var app = new AppItem
        {
            Id = "exe:sample",
            Name = "SampleApp",
            ExecutablePath = _dummyExePath,
            AppType = AppType.ExtraSource,
            IsRecentlyUsed = false,
            LastLaunchedUtc = null
        };

        var result = await service.LaunchAppAsync(app);

        result.Should().BeFalse();
        app.IsRecentlyUsed.Should().BeFalse();
        app.LastLaunchedUtc.Should().BeNull();
    }

    [Fact]
    public async Task LaunchAppAsync_MsixPackage_NonExistentFamily_ReturnsFalseSafely()
    {
        var service = new AppLauncherService(_ => true);
        var app = new AppItem
        {
            Id = $"uwp:NonExistent.LaunchMeTest.{Guid.NewGuid():N}",
            Name = "NonExistentUwp",
            ExecutablePath = string.Empty,
            AppType = AppType.MsixPackage
        };

        var result = await service.LaunchAppAsync(app);

        result.Should().BeFalse();
        app.IsRecentlyUsed.Should().BeFalse();
        app.LastLaunchedUtc.Should().BeNull();
    }

    [Fact]
    public void LaunchTarget_ExistingDirectory_InvokesProcessStarter()
    {
        ProcessStartInfo? capturedPsi = null;
        var service = new AppLauncherService(psi =>
        {
            capturedPsi = psi;
            return true;
        });

        var result = service.LaunchTarget(_tempDir);

        result.Should().BeTrue();
        capturedPsi.Should().NotBeNull();
        capturedPsi!.FileName.Should().Be(_tempDir);
        capturedPsi.UseShellExecute.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://example.com/path")]
    [InlineData("https://launchme.app/docs?tab=overview")]
    public void LaunchTarget_ValidHttpAndHttpsUrls_InvokesProcessStarter(string url)
    {
        ProcessStartInfo? capturedPsi = null;
        var service = new AppLauncherService(psi =>
        {
            capturedPsi = psi;
            return true;
        });

        var result = service.LaunchTarget(url);

        result.Should().BeTrue();
        capturedPsi.Should().NotBeNull();
        capturedPsi!.FileName.Should().Be(url);
        capturedPsi.UseShellExecute.Should().BeTrue();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/Windows/System32/cmd.exe")]
    [InlineData("ms-msdt:/id PCWDiagnostic")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("cmd:calc.exe")]
    [InlineData("ftp://example.com/archive.zip")]
    public void LaunchTarget_DangerousOrNonHttpSchemes_Rejected(string dangerousTarget)
    {
        var invoked = false;
        var service = new AppLauncherService(_ =>
        {
            invoked = true;
            return true;
        });

        var result = service.LaunchTarget(dangerousTarget);

        result.Should().BeFalse();
        invoked.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void LaunchTarget_EmptyOrWhitespace_ReturnsFalse(string target)
    {
        var invoked = false;
        var service = new AppLauncherService(_ =>
        {
            invoked = true;
            return true;
        });

        service.LaunchTarget(target).Should().BeFalse();
        invoked.Should().BeFalse();
    }

    [Fact]
    public void LaunchTarget_MissingFile_ReturnsFalse()
    {
        var invoked = false;
        var service = new AppLauncherService(_ =>
        {
            invoked = true;
            return true;
        });

        var missingPath = Path.Combine(_tempDir, "DoesNotExist.exe");
        var result = service.LaunchTarget(missingPath);

        result.Should().BeFalse();
        invoked.Should().BeFalse();
    }

    [Fact]
    public void LaunchTarget_ProcessStarterThrows_ReturnsFalseSafely()
    {
        var service = new AppLauncherService(_ => throw new InvalidOperationException("Simulated OS failure"));

        var result = service.LaunchTarget(_dummyExePath);

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
