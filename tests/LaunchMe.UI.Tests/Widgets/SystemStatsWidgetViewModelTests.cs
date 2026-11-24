using FluentAssertions;
using LaunchMe.UI.Widgets.SystemStats;
using Moq;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

public sealed class SystemStatsWidgetViewModelTests
{
    private readonly Mock<ISystemStatsProvider> _providerMock = new();

    [Fact]
    public void UpdateFromSnapshot_UnderNormalLoad_ClassifiesNormalStatus()
    {
        // Arrange
        using var vm = new SystemStatsWidgetViewModel(_providerMock.Object, startActive: false);
        var snapshot = new SystemMetricsSnapshot
        {
            CpuUsagePercent = 25.0,
            UsedMemoryBytes = 4L * 1024 * 1024 * 1024,
            TotalMemoryBytes = 16L * 1024 * 1024 * 1024, // 25% RAM
            NetworkUploadKbps = 100.0,
            NetworkDownloadKbps = 2500.0
        };

        // Act
        vm.UpdateFromSnapshot(snapshot);

        // Assert
        vm.CpuUsagePercent.Should().Be(25.0);
        vm.CpuUsageFormatted.Should().Be("25%");
        vm.CpuStatus.Should().Be(MetricStatusLevel.Normal);
        vm.MemoryStatus.Should().Be(MetricStatusLevel.Normal);
        vm.NetworkFormatted.Should().Contain("MB/s");
    }

    [Fact]
    public void UpdateFromSnapshot_UnderHighLoad_ClassifiesWarningAndCritical()
    {
        // Arrange
        using var vm = new SystemStatsWidgetViewModel(_providerMock.Object, startActive: false);
        var warningSnapshot = new SystemMetricsSnapshot
        {
            CpuUsagePercent = 75.0,
            UsedMemoryBytes = 13L * 1024 * 1024 * 1024,
            TotalMemoryBytes = 16L * 1024 * 1024 * 1024 // ~81% RAM
        };

        // Act 1: Warning
        vm.UpdateFromSnapshot(warningSnapshot);

        // Assert 1
        vm.CpuStatus.Should().Be(MetricStatusLevel.Warning);
        vm.MemoryStatus.Should().Be(MetricStatusLevel.Warning);

        // Act 2: Critical (> 90%)
        var criticalSnapshot = new SystemMetricsSnapshot
        {
            CpuUsagePercent = 95.0,
            UsedMemoryBytes = 15L * 1024 * 1024 * 1024,
            TotalMemoryBytes = 16L * 1024 * 1024 * 1024
        };
        vm.UpdateFromSnapshot(criticalSnapshot);

        // Assert 2
        vm.CpuStatus.Should().Be(MetricStatusLevel.Critical);
        vm.MemoryStatus.Should().Be(MetricStatusLevel.Critical);
    }
}
