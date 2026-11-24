using System.Runtime.InteropServices;
using LaunchMe.Platform.Win32.Metrics;
using LaunchMe.Platform.Win32.Native;
using Xunit;

namespace LaunchMe.Platform.Tests;

public class SystemMetricsTests
{
    [Fact]
    public void GetSystemTimes_ReturnsValidTimes()
    {
        bool success = Win32Interop.GetSystemTimes(out var idle, out var kernel, out var user);
        Assert.True(success);
        Assert.True(idle.ToUInt64() > 0);
        Assert.True(kernel.ToUInt64() > 0);
        Assert.True(user.ToUInt64() > 0);
    }

    [Fact]
    public void GlobalMemoryStatusEx_ReturnsValidMemoryMetrics()
    {
        var memStatus = new Win32Interop.MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<Win32Interop.MEMORYSTATUSEX>()
        };

        bool success = Win32Interop.GlobalMemoryStatusEx(ref memStatus);
        Assert.True(success);
        Assert.True(memStatus.ullTotalPhys > 0);
        Assert.True(memStatus.ullAvailPhys > 0);
        Assert.InRange(memStatus.dwMemoryLoad, 1u, 100u);
    }

    [Fact]
    public void GetIfTable2_CanEnumerateInterfaces()
    {
        int result = Win32Interop.GetIfTable2(out nint tablePtr);
        Assert.Equal(0, result);
        Assert.NotEqual(nint.Zero, tablePtr);

        try
        {
            uint numEntries = (uint)Marshal.ReadInt32(tablePtr);
            Assert.True(numEntries > 0);

            nint rowPtr = nint.Add(tablePtr, 8); // 8-byte aligned on x64
            int rowSize = Marshal.SizeOf<Win32Interop.MIB_IF_ROW2>();

            for (int i = 0; i < numEntries; i++)
            {
                var row = Marshal.PtrToStructure<Win32Interop.MIB_IF_ROW2>(rowPtr);
                Assert.True(row.InterfaceIndex > 0);
                rowPtr = nint.Add(rowPtr, rowSize);
            }
        }
        finally
        {
            Win32Interop.FreeMibTable(tablePtr);
        }
    }

    [Fact]
    public void Win32SystemMetricsService_ProvidesValidCpuMetrics()
    {
        using var service = new Win32SystemMetricsService();
        var cpu = service.GetCpuMetrics();

        Assert.NotNull(cpu);
        Assert.InRange(cpu.UsagePercentage, 0.0, 100.0);
        Assert.True(cpu.IdleTicks > 0);
        Assert.True(cpu.KernelTicks > 0);
        Assert.True(cpu.UserTicks > 0);
    }

    [Fact]
    public void Win32SystemMetricsService_ProvidesValidMemoryMetrics()
    {
        using var service = new Win32SystemMetricsService();
        var memory = service.GetMemoryMetrics();

        Assert.NotNull(memory);
        Assert.InRange(memory.MemoryLoadPercentage, 1.0, 100.0);
        Assert.True(memory.TotalPhysicalBytes > 0);
        Assert.True(memory.AvailablePhysicalBytes > 0);
        Assert.True(memory.UsedPhysicalBytes > 0);
        Assert.True(memory.TotalPhysicalBytes >= memory.AvailablePhysicalBytes);
        Assert.Equal(memory.TotalPhysicalBytes - memory.AvailablePhysicalBytes, memory.UsedPhysicalBytes);
    }

    [Fact]
    public void Win32SystemMetricsService_ProvidesValidNetworkMetrics()
    {
        using var service = new Win32SystemMetricsService();
        var network = service.GetNetworkMetrics();

        Assert.NotNull(network);
        Assert.NotEmpty(network.Interfaces);
        Assert.True(network.TotalBytesReceived > 0);

        foreach (var iface in network.Interfaces)
        {
            Assert.True(iface.InterfaceIndex > 0);
        }
    }

    [Fact]
    public async Task Win32SystemMetricsService_CalculatesNetworkRates()
    {
        using var service = new Win32SystemMetricsService();
        service.GetNetworkMetrics();

        await Task.Delay(50);

        var net2 = service.GetNetworkMetrics();
        Assert.True(net2.BytesReceivedPerSecond >= 0.0);
        Assert.True(net2.BytesSentPerSecond >= 0.0);
    }

    [Fact]
    public void Win32SystemMetricsService_ReturnsCompleteSnapshot()
    {
        using var service = new Win32SystemMetricsService();
        var snapshot = service.GetCurrentSnapshot();

        Assert.NotNull(snapshot);
        Assert.True(snapshot.Timestamp <= DateTime.UtcNow);
        Assert.True(snapshot.Timestamp >= DateTime.UtcNow.AddMinutes(-1));
        Assert.NotNull(snapshot.Cpu);
        Assert.NotNull(snapshot.Memory);
        Assert.NotNull(snapshot.Network);
    }

    [Fact]
    public void Win32SystemMetricsService_DisposedThrowsException()
    {
        var service = new Win32SystemMetricsService();
        service.Dispose();

        Assert.Throws<ObjectDisposedException>(() => service.GetCpuMetrics());
        Assert.Throws<ObjectDisposedException>(() => service.GetMemoryMetrics());
        Assert.Throws<ObjectDisposedException>(() => service.GetNetworkMetrics());
        Assert.Throws<ObjectDisposedException>(() => service.GetCurrentSnapshot());
    }
}
