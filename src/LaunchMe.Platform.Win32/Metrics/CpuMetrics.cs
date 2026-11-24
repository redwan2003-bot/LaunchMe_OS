namespace LaunchMe.Platform.Win32.Metrics;

/// <summary>
/// CPU performance metrics sampled from GetSystemTimes.
/// </summary>
public sealed record CpuMetrics(
    double UsagePercentage,
    ulong IdleTicks,
    ulong KernelTicks,
    ulong UserTicks);
