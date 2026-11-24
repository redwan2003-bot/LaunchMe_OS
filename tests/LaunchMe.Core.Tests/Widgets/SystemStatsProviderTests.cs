using FluentAssertions;
using LaunchMe.Core.Widgets.SystemStats;
using Xunit;

namespace LaunchMe.Core.Tests.Widgets;

public sealed class SystemStatsProviderTests
{
    private readonly ISystemStatsProvider _provider = new SystemStatsProvider();

    [Fact]
    public async Task GetSnapshotAsync_ReturnsValidMetrics()
    {
        var snapshot = await _provider.GetSnapshotAsync();

        snapshot.Should().NotBeNull();
        snapshot.Cpu.OverallPercent.Should().BeInRange(0.0, 100.0);
        snapshot.Cpu.CoreCount.Should().BeGreaterThan(0);
        snapshot.Cpu.PerCorePercent.Should().HaveCount(snapshot.Cpu.CoreCount);

        snapshot.Memory.TotalPhysicalBytes.Should().BeGreaterThan(0);
        snapshot.Memory.UsedPhysicalBytes.Should().BeGreaterThan(0);
        snapshot.Memory.UsagePercent.Should().BeInRange(0.0, 100.0);
        snapshot.Memory.FormattedTotal.Should().NotBeNullOrWhiteSpace();

        snapshot.Network.FormattedDownloadRate.Should().NotBeNullOrWhiteSpace();
        snapshot.Network.FormattedUploadRate.Should().NotBeNullOrWhiteSpace();

        snapshot.Disks.Should().NotBeEmpty();
        snapshot.Disks[0].TotalBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordMetrics_MaintainsHistoryBuffer()
    {
        _provider.RecordMetrics(25.0, 50.0);
        _provider.RecordMetrics(30.0, 52.0);

        var cpuHistory = _provider.GetRecentCpuHistory(10);
        var memHistory = _provider.GetRecentMemoryHistory(10);

        cpuHistory.Should().NotBeEmpty();
        memHistory.Should().NotBeEmpty();
    }
}
