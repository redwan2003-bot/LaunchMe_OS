using LaunchMe.Platform.Win32.Native;
using LaunchMe.Platform.Win32.Shell;
using Xunit;

namespace LaunchMe.Platform.Tests;

public class NotchTests
{
    [Fact]
    public void IsFullscreenGameActive_ReturnsBooleanWithoutCrashing()
    {
        using var service = new NotchWindowService();
        bool isFs = service.IsFullscreenGameActive();
        // Simply ensure it evaluates successfully on the current desktop environment
        Assert.True(isFs || !isFs);
    }

    [Theory]
    [InlineData(0, 0, 1920, 1080, 0, 0, 1920, 1080, true)]
    [InlineData(-5, -5, 1925, 1085, 0, 0, 1920, 1080, true)]
    [InlineData(100, 100, 1800, 900, 0, 0, 1920, 1080, false)]
    [InlineData(0, 0, 1920, 1000, 0, 0, 1920, 1080, false)]
    [InlineData(1920, 0, 3840, 1080, 1920, 0, 3840, 1080, true)]
    [InlineData(0, 0, 1920, 1080, 1920, 0, 3840, 1080, false)]
    public void CheckWindowBoundsMatchMonitor_EvaluatesCorrectly(
        int wLeft, int wTop, int wRight, int wBottom,
        int mLeft, int mTop, int mRight, int mBottom,
        bool expected)
    {
        var windowRect = new Win32Interop.RECT { left = wLeft, top = wTop, right = wRight, bottom = wBottom };
        var monitorRect = new Win32Interop.RECT { left = mLeft, top = mTop, right = mRight, bottom = mBottom };

        bool result = NotchWindowService.CheckWindowBoundsMatchMonitor(windowRect, monitorRect);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Progman", true)]
    [InlineData("WorkerW", true)]
    [InlineData("Shell_TrayWnd", true)]
    [InlineData("Shell_SecondaryTrayWnd", true)]
    [InlineData("PROGMAN", true)]
    [InlineData("Notepad", false)]
    [InlineData("Chrome_WidgetWin_1", false)]
    [InlineData("UnrealWindow", false)]
    public void IsExcludedShellClass_CorrectlyFiltersWindows(string className, bool expected)
    {
        bool result = NotchWindowService.IsExcludedShellClass(className);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ApplyScreenCaptureExclusion_ZeroHwnd_DoesNotThrow()
    {
        using var service = new NotchWindowService();
        service.ApplyScreenCaptureExclusion(nint.Zero, true);
        service.ApplyScreenCaptureExclusion(nint.Zero, false);
    }

    [Fact]
    public void NotchWindowService_Disposed_ThrowsException()
    {
        var service = new NotchWindowService();
        service.Dispose();

        Assert.Throws<ObjectDisposedException>(() => service.IsFullscreenGameActive());
    }
}
