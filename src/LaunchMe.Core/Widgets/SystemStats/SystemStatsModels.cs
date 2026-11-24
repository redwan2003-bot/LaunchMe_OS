using System.Globalization;

namespace LaunchMe.Core.Widgets.SystemStats;

public sealed record CpuMetrics
{
    public required double OverallPercent { get; init; }
    public required int CoreCount { get; init; }
    public required IReadOnlyList<double> PerCorePercent { get; init; }
}

public sealed record MemoryMetrics
{
    public required long TotalPhysicalBytes { get; init; }
    public required long UsedPhysicalBytes { get; init; }
    public required long AvailablePhysicalBytes { get; init; }
    public required double UsagePercent { get; init; }

    public string FormattedUsed => FormatBytes(UsedPhysicalBytes);
    public string FormattedTotal => FormatBytes(TotalPhysicalBytes);

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return $"{(double)bytes / (1024 * 1024 * 1024):F1} GB";
        if (bytes >= 1024L * 1024)
            return $"{(double)bytes / (1024 * 1024):F0} MB";
        return $"{bytes / 1024} KB";
    }
}

public sealed record NetworkMetrics
{
    public required long DownloadBytesPerSec { get; init; }
    public required long UploadBytesPerSec { get; init; }

    public string FormattedDownloadRate => FormatTransferRate(DownloadBytesPerSec);
    public string FormattedUploadRate => FormatTransferRate(UploadBytesPerSec);

    private static string FormatTransferRate(long bps)
    {
        if (bps >= 1024 * 1024)
            return string.Create(CultureInfo.InvariantCulture, $"{(double)bps / (1024 * 1024):F1} MB/s");
        if (bps >= 1024)
            return string.Create(CultureInfo.InvariantCulture, $"{(double)bps / 1024:F0} KB/s");
        return $"{bps} B/s";
    }
}

public sealed record DiskMetrics
{
    public required string DriveName { get; init; }
    public required long TotalBytes { get; init; }
    public required long FreeBytes { get; init; }
    public long UsedBytes => TotalBytes - FreeBytes;
    public double UsagePercent => TotalBytes > 0 ? Math.Round((double)UsedBytes / TotalBytes * 100.0, 1) : 0.0;
}

public sealed record SystemStatsSnapshot
{
    public required DateTimeOffset Timestamp { get; init; }
    public required CpuMetrics Cpu { get; init; }
    public required MemoryMetrics Memory { get; init; }
    public required NetworkMetrics Network { get; init; }
    public required IReadOnlyList<DiskMetrics> Disks { get; init; }
}
