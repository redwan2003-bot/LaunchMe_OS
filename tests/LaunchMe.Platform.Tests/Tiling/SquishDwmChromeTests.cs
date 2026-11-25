using FluentAssertions;
using LaunchMe.Contracts.Tiling;
using LaunchMe.Platform.Win32.Native;
using LaunchMe.Platform.Win32.Tiling;
using Xunit;

namespace LaunchMe.Platform.Win32.Tests.Tiling;

public class SquishDwmChromeTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    [InlineData(-1, true)]
    [InlineData(0x12345678, false)]
    [InlineData(0x12345678, true)]
    public void ApplyWindows11SquishChrome_ZeroOrInvalidHwnd_DoesNotThrow(long rawHwnd, bool isFocused)
    {
        var hwnd = new nint(rawHwnd);

        var act = () => Win32TilingDispatcher.ApplyWindows11SquishChrome(hwnd, isFocused);

        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyWindows11SquishChrome_CustomColorAndInvalidHwnd_DoesNotThrow()
    {
        var act = () => Win32TilingDispatcher.ApplyWindows11SquishChrome(
            new nint(0x0BADF00D),
            isFocused: true,
            activeBgrColor: 0x00FFAA00);

        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyLayout_MultipleInvalidHwnds_AppliesChromeSafelyWithoutThrowing()
    {
        var dispatcher = new Win32TilingDispatcher();
        var layout = new[]
        {
            (nint.Zero, new WindowRect(0, 0, 960, 1080)),
            (new nint(0x12345678), new WindowRect(960, 0, 960, 1080))
        };

        var act = () => dispatcher.ApplyLayout(layout);

        act.Should().NotThrow();
    }

    [Fact]
    public void Win32Interop_DwmSquishChromeConstants_MatchWindows11Specification()
    {
        Win32Interop.DWMWA_WINDOW_CORNER_PREFERENCE.Should().Be(33u);
        Win32Interop.DWMWCP_ROUND.Should().Be(2);
        Win32Interop.DWMWA_BORDER_COLOR.Should().Be(34u);
        Win32Interop.DWMWA_COLOR_DEFAULT.Should().Be(0xFFFFFFFFu);
        Win32Interop.DWMWA_COLOR_NONE.Should().Be(0xFFFFFFFEu);
    }
}
