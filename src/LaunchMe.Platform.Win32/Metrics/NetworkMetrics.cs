namespace LaunchMe.Platform.Win32.Metrics;

/// <summary>
/// Metrics for an individual network interface sampled via GetIfTable2.
/// </summary>
public sealed record NetworkInterfaceMetric(
    uint InterfaceIndex,
    string Alias,
    string Description,
    ulong InOctets,
    ulong OutOctets,
    ulong TransmitSpeedBps,
    ulong ReceiveSpeedBps,
    int OperStatus);

/// <summary>
/// Aggregated network metrics across active interfaces.
/// </summary>
public sealed record NetworkMetrics(
    ulong TotalBytesReceived,
    ulong TotalBytesSent,
    double BytesReceivedPerSecond,
    double BytesSentPerSecond,
    IReadOnlyList<NetworkInterfaceMetric> Interfaces);
