using System.Runtime.InteropServices;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Metrics;

/// <summary>
/// Native Win32 implementation of ISystemMetricsService using GetSystemTimes,
/// GlobalMemoryStatusEx, and GetIfTable2.
/// </summary>
public sealed class Win32SystemMetricsService : ISystemMetricsService
{
    private readonly object _lock = new();

    private ulong _lastIdle;
    private ulong _lastKernel;
    private ulong _lastUser;
    private bool _hasCpuBaseline;

    private ulong _lastTotalIn;
    private ulong _lastTotalOut;
    private DateTime _lastNetworkTime;
    private bool _hasNetworkBaseline;

    private bool _disposed;

    public Win32SystemMetricsService()
    {
        InitializeBaselines();
    }

    private void InitializeBaselines()
    {
        lock (_lock)
        {
            if (Win32Interop.GetSystemTimes(out var idle, out var kernel, out var user))
            {
                _lastIdle = idle.ToUInt64();
                _lastKernel = kernel.ToUInt64();
                _lastUser = user.ToUInt64();
                _hasCpuBaseline = true;
            }

            int result = Win32Interop.GetIfTable2(out nint tablePtr);
            if (result == 0 && tablePtr != nint.Zero)
            {
                try
                {
                    uint numEntries = (uint)Marshal.ReadInt32(tablePtr);
                    nint rowPtr = nint.Add(tablePtr, 8);
                    int rowSize = Marshal.SizeOf<Win32Interop.MIB_IF_ROW2>();

                    ulong totalIn = 0;
                    ulong totalOut = 0;

                    for (int i = 0; i < numEntries; i++)
                    {
                        var row = Marshal.PtrToStructure<Win32Interop.MIB_IF_ROW2>(rowPtr);
                        totalIn += row.InOctets;
                        totalOut += row.OutOctets;
                        rowPtr = nint.Add(rowPtr, rowSize);
                    }

                    _lastTotalIn = totalIn;
                    _lastTotalOut = totalOut;
                    _lastNetworkTime = DateTime.UtcNow;
                    _hasNetworkBaseline = true;
                }
                finally
                {
                    Win32Interop.FreeMibTable(tablePtr);
                }
            }
        }
    }

    public CpuMetrics GetCpuMetrics()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (!Win32Interop.GetSystemTimes(out var idleFt, out var kernelFt, out var userFt))
            {
                return new CpuMetrics(0.0, 0, 0, 0);
            }

            ulong currentIdle = idleFt.ToUInt64();
            ulong currentKernel = kernelFt.ToUInt64();
            ulong currentUser = userFt.ToUInt64();

            if (!_hasCpuBaseline)
            {
                _lastIdle = currentIdle;
                _lastKernel = currentKernel;
                _lastUser = currentUser;
                _hasCpuBaseline = true;
                return new CpuMetrics(0.0, currentIdle, currentKernel, currentUser);
            }

            ulong deltaIdle = currentIdle >= _lastIdle ? currentIdle - _lastIdle : 0;
            ulong deltaKernel = currentKernel >= _lastKernel ? currentKernel - _lastKernel : 0;
            ulong deltaUser = currentUser >= _lastUser ? currentUser - _lastUser : 0;
            ulong deltaTotal = deltaKernel + deltaUser;

            double usagePercentage = 0.0;
            if (deltaTotal > 0)
            {
                double fraction = (double)deltaIdle / (double)deltaTotal;
                usagePercentage = Math.Clamp((1.0 - fraction) * 100.0, 0.0, 100.0);
            }

            _lastIdle = currentIdle;
            _lastKernel = currentKernel;
            _lastUser = currentUser;

            return new CpuMetrics(usagePercentage, currentIdle, currentKernel, currentUser);
        }
    }

    public MemoryMetrics GetMemoryMetrics()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var memStatus = new Win32Interop.MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<Win32Interop.MEMORYSTATUSEX>()
        };

        if (!Win32Interop.GlobalMemoryStatusEx(ref memStatus))
        {
            return new MemoryMetrics(0.0, 0, 0, 0, 0, 0);
        }

        ulong totalPhys = memStatus.ullTotalPhys;
        ulong availPhys = memStatus.ullAvailPhys;
        ulong usedPhys = totalPhys >= availPhys ? totalPhys - availPhys : 0;
        double loadPercentage = memStatus.dwMemoryLoad;

        return new MemoryMetrics(
            loadPercentage,
            totalPhys,
            availPhys,
            usedPhys,
            memStatus.ullTotalPageFile,
            memStatus.ullAvailPageFile);
    }

    public NetworkMetrics GetNetworkMetrics()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            int result = Win32Interop.GetIfTable2(out nint tablePtr);
            if (result != 0 || tablePtr == nint.Zero)
            {
                return new NetworkMetrics(0, 0, 0.0, 0.0, []);
            }

            try
            {
                uint numEntries = (uint)Marshal.ReadInt32(tablePtr);
                nint rowPtr = nint.Add(tablePtr, 8);
                int rowSize = Marshal.SizeOf<Win32Interop.MIB_IF_ROW2>();

                var interfaces = new List<NetworkInterfaceMetric>((int)numEntries);
                ulong totalIn = 0;
                ulong totalOut = 0;

                for (int i = 0; i < numEntries; i++)
                {
                    var row = Marshal.PtrToStructure<Win32Interop.MIB_IF_ROW2>(rowPtr);

                    interfaces.Add(new NetworkInterfaceMetric(
                        row.InterfaceIndex,
                        row.Alias ?? string.Empty,
                        row.Description ?? string.Empty,
                        row.InOctets,
                        row.OutOctets,
                        row.TransmitLinkSpeed,
                        row.ReceiveLinkSpeed,
                        row.OperStatus));

                    totalIn += row.InOctets;
                    totalOut += row.OutOctets;

                    rowPtr = nint.Add(rowPtr, rowSize);
                }

                DateTime now = DateTime.UtcNow;
                double bytesInPerSec = 0.0;
                double bytesOutPerSec = 0.0;

                if (_hasNetworkBaseline)
                {
                    double elapsedSeconds = (now - _lastNetworkTime).TotalSeconds;
                    if (elapsedSeconds > 0.001)
                    {
                        ulong deltaIn = totalIn >= _lastTotalIn ? totalIn - _lastTotalIn : 0;
                        ulong deltaOut = totalOut >= _lastTotalOut ? totalOut - _lastTotalOut : 0;
                        bytesInPerSec = deltaIn / elapsedSeconds;
                        bytesOutPerSec = deltaOut / elapsedSeconds;
                    }
                }

                _lastTotalIn = totalIn;
                _lastTotalOut = totalOut;
                _lastNetworkTime = now;
                _hasNetworkBaseline = true;

                return new NetworkMetrics(totalIn, totalOut, bytesInPerSec, bytesOutPerSec, interfaces);
            }
            finally
            {
                Win32Interop.FreeMibTable(tablePtr);
            }
        }
    }

    public SystemPerformanceSnapshot GetCurrentSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var cpu = GetCpuMetrics();
        var memory = GetMemoryMetrics();
        var network = GetNetworkMetrics();

        return new SystemPerformanceSnapshot(DateTime.UtcNow, cpu, memory, network);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
