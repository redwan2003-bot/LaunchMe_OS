using LaunchMe.Platform.Win32.Shell;
using Xunit;

namespace LaunchMe.Platform.Tests;

public sealed class DockShellTests
{
    [Fact]
    public void DockShellService_DisposesGracefully()
    {
        var service = new DockShellService();
        service.Dispose();
        // Subsequent dispose should be safe
        service.Dispose();
    }

    [Fact]
    public void DockShellService_NullHwndHandledGracefully()
    {
        using var service = new DockShellService();
        // Passing Zero hwnd should safely return without error
        service.RegisterAppBar(nint.Zero, nint.Zero, 60, true);
        service.UnregisterAppBar(nint.Zero);
        service.UpdateAppBarPos(nint.Zero, nint.Zero, 60, true);
    }
}
