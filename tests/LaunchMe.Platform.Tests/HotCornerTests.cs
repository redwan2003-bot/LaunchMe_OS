using FluentAssertions;
using LaunchMe.Contracts.Triggers;
using LaunchMe.Platform.Win32.Gestures;
using LaunchMe.Platform.Win32.Native;
using Xunit;

namespace LaunchMe.Platform.Tests;

public class HotCornerTests
{
    private readonly Win32Interop.RECT _monitor = new()
    {
        left = 0,
        top = 0,
        right = 1920,
        bottom = 1080
    };

    [Theory]
    [InlineData(0, 0, HotCornerLocation.TopLeft)]
    [InlineData(4, 4, HotCornerLocation.TopLeft)]
    [InlineData(1920, 0, HotCornerLocation.TopRight)]
    [InlineData(1916, 4, HotCornerLocation.TopRight)]
    [InlineData(0, 1080, HotCornerLocation.BottomLeft)]
    [InlineData(4, 1076, HotCornerLocation.BottomLeft)]
    [InlineData(1920, 1080, HotCornerLocation.BottomRight)]
    [InlineData(1916, 1076, HotCornerLocation.BottomRight)]
    public void EvaluateCorner_InsideThreshold_ReturnsExpectedCorner(int x, int y, HotCornerLocation expected)
    {
        var result = HotCornerHitTester.EvaluateCorner(x, y, _monitor, 5);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(960, 540)]
    [InlineData(1900, 1000)]
    [InlineData(-10, -10)]
    public void EvaluateCorner_OutsideThreshold_ReturnsNull(int x, int y)
    {
        var result = HotCornerHitTester.EvaluateCorner(x, y, _monitor, 5);
        result.Should().BeNull();
    }

    [Fact]
    public void FindCornerAcrossMonitors_SecondaryMonitor_DetectsCorrectly()
    {
        var monitors = new[]
        {
            new Win32Interop.RECT { left = 0, top = 0, right = 1920, bottom = 1080 },
            new Win32Interop.RECT { left = 1920, top = 0, right = 3840, bottom = 1080 }
        };

        var corner = HotCornerHitTester.FindCornerAcrossMonitors(3840, 0, monitors, 5);
        corner.Should().Be(HotCornerLocation.TopRight);
    }

    [Fact]
    public void DwellTracker_Under150Ms_DoesNotTrigger()
    {
        var tracker = new HotCornerDwellTracker(150);
        var t0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        tracker.Update(HotCornerLocation.TopLeft, t0, out _).Should().BeFalse();
        tracker.Update(HotCornerLocation.TopLeft, t0.AddMilliseconds(100), out _).Should().BeFalse();
        tracker.HasTriggered.Should().BeFalse();
    }

    [Fact]
    public void DwellTracker_At150Ms_TriggersExactlyOnce()
    {
        var tracker = new HotCornerDwellTracker(150);
        var t0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        tracker.Update(HotCornerLocation.TopLeft, t0, out _).Should().BeFalse();

        bool triggered = tracker.Update(HotCornerLocation.TopLeft, t0.AddMilliseconds(150), out var corner);
        triggered.Should().BeTrue();
        corner.Should().Be(HotCornerLocation.TopLeft);
        tracker.HasTriggered.Should().BeTrue();

        bool secondPoll = tracker.Update(HotCornerLocation.TopLeft, t0.AddMilliseconds(250), out _);
        secondPoll.Should().BeFalse();
    }

    [Fact]
    public void DwellTracker_ExitCorner_ResetsDwell()
    {
        var tracker = new HotCornerDwellTracker(150);
        var t0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        tracker.Update(HotCornerLocation.TopLeft, t0, out _);
        tracker.Update(null, t0.AddMilliseconds(50), out _);

        tracker.ActiveCorner.Should().BeNull();
        tracker.HasTriggered.Should().BeFalse();

        tracker.Update(HotCornerLocation.TopLeft, t0.AddMilliseconds(100), out _);
        tracker.Update(HotCornerLocation.TopLeft, t0.AddMilliseconds(200), out _).Should().BeFalse();

        tracker.Update(HotCornerLocation.TopLeft, t0.AddMilliseconds(250), out var corner).Should().BeTrue();
        corner.Should().Be(HotCornerLocation.TopLeft);
    }
}
