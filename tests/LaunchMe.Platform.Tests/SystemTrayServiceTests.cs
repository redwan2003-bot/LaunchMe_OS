using FluentAssertions;
using LaunchMe.Platform.Win32.Native;
using LaunchMe.Platform.Win32.Shell;
using Xunit;

namespace LaunchMe.Platform.Tests;

public class SystemTrayServiceTests
{
    [Fact]
    public void NewService_IsNotInitialized()
    {
        using var service = new SystemTrayService();

        service.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void Initialize_AndDispose_LifecycleTransitionsCleanly()
    {
        using var service = new SystemTrayService();

        service.Initialize("LaunchMe-Win Test");
        service.IsInitialized.Should().BeTrue();

        // Second initialize should remain initialized idempotently (updates tooltip via NIM_MODIFY).
        service.Initialize("LaunchMe-Win Updated");
        service.IsInitialized.Should().BeTrue();

        service.Dispose();
        service.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        var service = new SystemTrayService();
        service.Initialize("LaunchMe-Win");

        service.Dispose();
        Action secondDispose = () => service.Dispose();

        secondDispose.Should().NotThrow();
        service.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void Dispose_WithoutInitialize_IsIdempotent()
    {
        var service = new SystemTrayService();

        service.Dispose();
        Action secondDispose = () => service.Dispose();

        secondDispose.Should().NotThrow();
        service.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void Initialize_AfterDispose_ThrowsObjectDisposedException()
    {
        var service = new SystemTrayService();
        service.Dispose();

        Action act = () => service.Initialize("LaunchMe-Win");

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void DispatchCommand_ShowLauncher_RaisesShowLauncherRequested()
    {
        using var service = new SystemTrayService();
        int showCount = 0;
        int settingsCount = 0;
        int exitCount = 0;

        service.ShowLauncherRequested += (_, _) => showCount++;
        service.OpenSettingsRequested += (_, _) => settingsCount++;
        service.ExitRequested += (_, _) => exitCount++;

        bool handled = service.DispatchCommand(SystemTrayService.CommandShowLauncher);

        handled.Should().BeTrue();
        showCount.Should().Be(1);
        settingsCount.Should().Be(0);
        exitCount.Should().Be(0);
    }

    [Fact]
    public void DispatchCommand_OpenSettings_RaisesOpenSettingsRequested()
    {
        using var service = new SystemTrayService();
        int showCount = 0;
        int settingsCount = 0;
        int exitCount = 0;

        service.ShowLauncherRequested += (_, _) => showCount++;
        service.OpenSettingsRequested += (_, _) => settingsCount++;
        service.ExitRequested += (_, _) => exitCount++;

        bool handled = service.DispatchCommand(SystemTrayService.CommandOpenSettings);

        handled.Should().BeTrue();
        showCount.Should().Be(0);
        settingsCount.Should().Be(1);
        exitCount.Should().Be(0);
    }

    [Fact]
    public void DispatchCommand_Exit_RaisesExitRequested()
    {
        using var service = new SystemTrayService();
        int showCount = 0;
        int settingsCount = 0;
        int exitCount = 0;

        service.ShowLauncherRequested += (_, _) => showCount++;
        service.OpenSettingsRequested += (_, _) => settingsCount++;
        service.ExitRequested += (_, _) => exitCount++;

        bool handled = service.DispatchCommand(SystemTrayService.CommandExit);

        handled.Should().BeTrue();
        showCount.Should().Be(0);
        settingsCount.Should().Be(0);
        exitCount.Should().Be(1);
    }

    [Fact]
    public void DispatchCommand_UnknownId_ReturnsFalseAndRaisesNothing()
    {
        using var service = new SystemTrayService();
        bool anyRaised = false;

        service.ShowLauncherRequested += (_, _) => anyRaised = true;
        service.OpenSettingsRequested += (_, _) => anyRaised = true;
        service.ExitRequested += (_, _) => anyRaised = true;

        bool handled = service.DispatchCommand(9999);

        handled.Should().BeFalse();
        anyRaised.Should().BeFalse();
    }

    [Theory]
    [InlineData(Win32Interop.WM_LBUTTONUP)]
    [InlineData(Win32Interop.WM_LBUTTONDBLCLK)]
    public void HandleTrayMouseMessage_LeftClickOrDoubleClick_RaisesShowLauncherRequested(uint mouseMessage)
    {
        using var service = new SystemTrayService();
        int showCount = 0;
        service.ShowLauncherRequested += (_, _) => showCount++;

        bool handled = service.HandleTrayMouseMessage(mouseMessage);

        handled.Should().BeTrue();
        showCount.Should().Be(1);
    }

    [Fact]
    public void HandleTrayMouseMessage_UnrecognizedMessage_ReturnsFalse()
    {
        using var service = new SystemTrayService();
        bool showRaised = false;
        service.ShowLauncherRequested += (_, _) => showRaised = true;

        bool handled = service.HandleTrayMouseMessage(Win32Interop.WM_MOUSEMOVE);

        handled.Should().BeFalse();
        showRaised.Should().BeFalse();
    }
}
