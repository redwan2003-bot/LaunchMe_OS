using FluentAssertions;
using LaunchMe.Contracts.Tiling;
using LaunchMe.Platform.Win32.Tiling;
using Moq;
using Xunit;

namespace LaunchMe.Platform.Win32.Tests.Tiling;

public class Win32WindowTrackerTests
{
    private readonly Mock<ITilingEngine> _engineMock;
    private readonly Mock<IWindowRuleService> _rulesMock;
    private readonly Mock<IWindowTilingDispatcher> _dispatcherMock;
    private readonly Mock<IFocusBorderService> _focusBorderMock;

    public Win32WindowTrackerTests()
    {
        _engineMock = new Mock<ITilingEngine>();
        _rulesMock = new Mock<IWindowRuleService>();
        _dispatcherMock = new Mock<IWindowTilingDispatcher>();
        _focusBorderMock = new Mock<IFocusBorderService>();
    }

    [Fact]
    public void StartAndStop_DoesNotThrow()
    {
        using var tracker = new Win32WindowTracker(
            _engineMock.Object,
            _rulesMock.Object,
            _dispatcherMock.Object,
            _focusBorderMock.Object);

        var actStart = () => tracker.Start();
        var actStop = () => tracker.Stop();

        actStart.Should().NotThrow();
        actStop.Should().NotThrow();
    }

    [Fact]
    public void HandleWindowDestroyed_UnregistersWindow_And_AppliesNewLayout()
    {
        using var tracker = new Win32WindowTracker(
            _engineMock.Object,
            _rulesMock.Object,
            _dispatcherMock.Object,
            _focusBorderMock.Object);

        var hwnd = new nint(12345);
        var area = new WindowRect(0, 0, 1920, 1080);

        _engineMock.Setup(e => e.ComputeLayout("primary", area, 1))
            .Returns(new List<(nint, WindowRect)>());

        tracker.HandleWindowDestroyed(hwnd, area, 1);

        _engineMock.Verify(e => e.UnregisterWindow(hwnd), Times.Once);
        _dispatcherMock.Verify(d => d.ApplyLayout(It.IsAny<IReadOnlyList<(nint, WindowRect)>>()), Times.Once);
    }

    [Fact]
    public void HandleForegroundChanged_AttachesFocusBorder_IfValidWindow()
    {
        using var tracker = new Win32WindowTracker(
            _engineMock.Object,
            _rulesMock.Object,
            _dispatcherMock.Object,
            _focusBorderMock.Object);

        // Invalid HWND should not crash or attach
        tracker.HandleForegroundChanged(nint.Zero);

        _focusBorderMock.Verify(f => f.AttachToWindow(It.IsAny<nint>(), It.IsAny<WindowRect>()), Times.Never);
    }
}
