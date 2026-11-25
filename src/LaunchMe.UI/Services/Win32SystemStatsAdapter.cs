using LaunchMe.Platform.Win32.Metrics;
using LaunchMe.UI.Widgets.SystemStats;

namespace LaunchMe.UI.Services;

/// <summary>
/// Adapts native Win32 <see cref="ISystemMetricsService"/> snapshots into UI <see cref="SystemMetricsSnapshot"/> instances.
/// </summary>
public sealed class Win32SystemStatsAdapter : ISystemStatsProvider
{
    private readonly ISystemMetricsService _metricsService;

    public Win32SystemStatsAdapter(ISystemMetricsService metricsService)
    {
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
    }

    public Task<SystemMetricsSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        SystemPerformanceSnapshot perf = _metricsService.GetCurrentSnapshot();
        var snapshot = new SystemMetricsSnapshot
        {
            CpuUsagePercent = perf.Cpu.UsagePercentage,
            UsedMemoryBytes = (long)Math.Min(perf.Memory.UsedPhysicalBytes, long.MaxValue),
            TotalMemoryBytes = (long)Math.Min(perf.Memory.TotalPhysicalBytes, long.MaxValue),
            NetworkUploadKbps = perf.Network.BytesSentPerSecond / 1024.0,
            NetworkDownloadKbps = perf.Network.BytesReceivedPerSecond / 1024.0
        };

        return Task.FromResult(snapshot);
    }
}
