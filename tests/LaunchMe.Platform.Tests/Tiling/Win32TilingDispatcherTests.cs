using FluentAssertions;
using LaunchMe.Contracts.Tiling;
using LaunchMe.Platform.Win32.Tiling;
using Xunit;

namespace LaunchMe.Platform.Win32.Tests.Tiling;

public class Win32TilingDispatcherTests
{
    [Fact]
    public void ApplyLayout_EmptyLayout_DoesNotThrow()
    {
        var dispatcher = new Win32TilingDispatcher();
        var act = () => dispatcher.ApplyLayout(Array.Empty<(nint, WindowRect)>());

        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyLayout_InvalidHwnd_DoesNotCrash()
    {
        var dispatcher = new Win32TilingDispatcher();
        var layout = new[] { (new nint(0x12345678), new WindowRect(0, 0, 800, 600)) };

        var act = () => dispatcher.ApplyLayout(layout);

        act.Should().NotThrow();
    }

    [Fact]
    public void HideAndRestoreWindow_InvalidHwnd_DoesNotCrash()
    {
        var dispatcher = new Win32TilingDispatcher();

        var actHide = () => dispatcher.HideWindow(new nint(0x9999));
        var actRestore = () => dispatcher.RestoreWindow(new nint(0x9999));

        actHide.Should().NotThrow();
        actRestore.Should().NotThrow();
    }
}
