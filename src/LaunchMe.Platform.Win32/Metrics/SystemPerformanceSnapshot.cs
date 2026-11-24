namespace LaunchMe.Platform.Win32.Metrics;

/// <summary>
/// Combined system performance snapshot containing CPU, Memory, and Network metrics.
/// </summary>
public sealed record SystemPerformanceSnapshot(
    DateTime Timestamp,
    CpuMetrics Cpu,
    MemoryMetrics Memory,
    NetworkMetrics Network);
