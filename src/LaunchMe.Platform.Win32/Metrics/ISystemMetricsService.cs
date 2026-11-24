namespace LaunchMe.Platform.Win32.Metrics;

/// <summary>
/// Service providing native Windows system performance metrics (CPU, Memory, Network).
/// </summary>
public interface ISystemMetricsService : IDisposable
{
    CpuMetrics GetCpuMetrics();
    MemoryMetrics GetMemoryMetrics();
    NetworkMetrics GetNetworkMetrics();
    SystemPerformanceSnapshot GetCurrentSnapshot();
}
