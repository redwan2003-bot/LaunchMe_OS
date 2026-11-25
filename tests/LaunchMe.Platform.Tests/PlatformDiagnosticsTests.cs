using FluentAssertions;
using LaunchMe.Platform.Win32.Shell;
using Xunit;

namespace LaunchMe.Platform.Tests;

public class PlatformDiagnosticsTests
{
    [Fact]
    public void CollectSnapshot_ReturnsValidSystemDpiAndScalePercentage()
    {
        var service = new PlatformDiagnosticsService();

        PlatformDiagnosticsSnapshot snapshot = service.CollectSnapshot();

        snapshot.SystemDpi.Should().BeGreaterThanOrEqualTo(96u);
        snapshot.ScalePercentage.Should().BeGreaterThanOrEqualTo(100.0);
        snapshot.ScalePercentage.Should().Be(Math.Round(snapshot.SystemDpi / 96.0 * 100.0, 2));
    }

    [Fact]
    public void CollectSnapshot_ReturnsAtLeastOneMonitor()
    {
        var service = new PlatformDiagnosticsService();

        PlatformDiagnosticsSnapshot snapshot = service.CollectSnapshot();

        snapshot.MonitorCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void CollectSnapshot_ReturnsNonEmptyOsVersion()
    {
        var service = new PlatformDiagnosticsService();

        PlatformDiagnosticsSnapshot snapshot = service.CollectSnapshot();

        snapshot.OsVersion.Should().NotBeNullOrWhiteSpace();
    }
}
