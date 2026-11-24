namespace LaunchMe.UI.Widgets.SystemStats;

public sealed class SystemMetricsSnapshot
{
    public double CpuUsagePercent { get; set; }
    public long UsedMemoryBytes { get; set; }
    public long TotalMemoryBytes { get; set; }
    public double UsedMemoryGb => Math.Round(UsedMemoryBytes / (1024.0 * 1024.0 * 1024.0), 1);
    public double TotalMemoryGb => Math.Round(TotalMemoryBytes / (1024.0 * 1024.0 * 1024.0), 1);
    public double MemoryUsagePercent => TotalMemoryBytes > 0 ? (UsedMemoryBytes * 100.0 / TotalMemoryBytes) : 0.0;
    public double NetworkUploadKbps { get; set; }
    public double NetworkDownloadKbps { get; set; }
}

public interface ISystemStatsProvider
{
    Task<SystemMetricsSnapshot> GetSnapshotAsync(CancellationToken ct = default);
}

public sealed class DefaultSystemStatsProvider : ISystemStatsProvider
{
    public Task<SystemMetricsSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        // Safe managed baseline for memory metrics
        long totalMemory = 16L * 1024 * 1024 * 1024; // 16 GB default physical reference
        long processMemory = Environment.WorkingSet;
        long simulatedUsedMemory = Math.Max(processMemory * 4, 6L * 1024 * 1024 * 1024); // baseline 6 GB load

        var snapshot = new SystemMetricsSnapshot
        {
            CpuUsagePercent = 14.5,
            UsedMemoryBytes = simulatedUsedMemory,
            TotalMemoryBytes = totalMemory,
            NetworkUploadKbps = 85.0,
            NetworkDownloadKbps = 340.0
        };

        return Task.FromResult(snapshot);
    }
}
