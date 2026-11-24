namespace LaunchMe.Platform.Win32.Metrics;

/// <summary>
/// Memory status metrics sampled from GlobalMemoryStatusEx.
/// </summary>
public sealed record MemoryMetrics(
    double MemoryLoadPercentage,
    ulong TotalPhysicalBytes,
    ulong AvailablePhysicalBytes,
    ulong UsedPhysicalBytes,
    ulong TotalPageFileBytes,
    ulong AvailablePageFileBytes);
