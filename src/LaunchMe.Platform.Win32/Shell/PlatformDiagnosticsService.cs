using System.Runtime.InteropServices;
using System.Security.Principal;
using LaunchMe.Platform.Win32.Native;

namespace LaunchMe.Platform.Win32.Shell;

/// <summary>
/// Snapshot of Windows platform display, DPI, elevation, and OS diagnostics.
/// </summary>
public sealed record PlatformDiagnosticsSnapshot(
    uint SystemDpi,
    double ScalePercentage,
    int MonitorCount,
    bool IsElevated,
    string OsVersion);

/// <summary>
/// Collects native Win32 display and runtime environment diagnostics.
/// </summary>
public sealed class PlatformDiagnosticsService
{
    public PlatformDiagnosticsSnapshot CollectSnapshot()
    {
        uint dpi = Win32Interop.GetDpiForSystem();
        if (dpi == 0)
        {
            dpi = 96;
        }

        double scalePercentage = Math.Round(dpi / 96.0 * 100.0, 2);
        int monitorCount = QueryMonitorCount();
        bool isElevated = IsCurrentProcessElevated();
        string osVersion = !string.IsNullOrWhiteSpace(RuntimeInformation.OSDescription)
            ? RuntimeInformation.OSDescription
            : Environment.OSVersion.VersionString;

        return new PlatformDiagnosticsSnapshot(
            SystemDpi: dpi,
            ScalePercentage: scalePercentage,
            MonitorCount: monitorCount,
            IsElevated: isElevated,
            OsVersion: osVersion);
    }

    private static int QueryMonitorCount()
    {
        int count = 0;
        Win32Interop.MonitorEnumProc callback = (nint _, nint _, ref Win32Interop.RECT _, nint _) =>
        {
            count++;
            return true;
        };

        Win32Interop.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
        GC.KeepAlive(callback);

        return count > 0
            ? count
            : Math.Max(1, Win32Interop.GetSystemMetrics(Win32Interop.SM_CMONITORS));
    }

    private static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
