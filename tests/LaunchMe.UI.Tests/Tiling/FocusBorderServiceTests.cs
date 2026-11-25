using FluentAssertions;
using LaunchMe.Contracts.Tiling;
using LaunchMe.UI.Tiling;
using Xunit;

namespace LaunchMe.UI.Tests.Tiling;

public class FocusBorderServiceTests
{
    [Fact]
    public void InitialState_IsHidden()
    {
        var service = new FocusBorderService();

        service.IsVisible.Should().BeFalse();
        service.AttachedHwnd.Should().Be(nint.Zero);
        service.CurrentBounds.Should().Be(WindowRect.Empty);
    }

    [Fact]
    public void AttachToWindow_ValidHwndAndBounds_SetsVisibleAndUpdatesBounds()
    {
        var service = new FocusBorderService();
        var hwnd = new nint(12345);
        var bounds = new WindowRect(100, 150, 800, 600);

        service.AttachToWindow(hwnd, bounds);

        service.IsVisible.Should().BeTrue();
        service.AttachedHwnd.Should().Be(hwnd);
        service.CurrentBounds.Should().Be(bounds);
    }

    [Fact]
    public void AttachToWindow_ZeroHwnd_HidesBorder()
    {
        var service = new FocusBorderService();
        service.AttachToWindow(new nint(12345), new WindowRect(100, 150, 800, 600));

        service.AttachToWindow(nint.Zero, new WindowRect(100, 150, 800, 600));

        service.IsVisible.Should().BeFalse();
        service.AttachedHwnd.Should().Be(nint.Zero);
    }

    [Fact]
    public void HideBorder_ResetsVisibilityAndBounds()
    {
        var service = new FocusBorderService();
        service.AttachToWindow(new nint(12345), new WindowRect(100, 150, 800, 600));

        service.HideBorder();

        service.IsVisible.Should().BeFalse();
        service.AttachedHwnd.Should().Be(nint.Zero);
        service.CurrentBounds.Should().Be(WindowRect.Empty);
    }
}
