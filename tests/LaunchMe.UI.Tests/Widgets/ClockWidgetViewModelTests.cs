using FluentAssertions;
using LaunchMe.UI.Widgets.Clock;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

public sealed class ClockWidgetViewModelTests
{
    [Fact]
    public void Display_24HourFormat_FormatsCorrectly()
    {
        // Arrange
        using var vm = new ClockWidgetViewModel(startActive: false);
        var testTime = new DateTime(2026, 11, 24, 14, 30, 45);

        // Act
        vm.Is24HourFormat = true;
        vm.ShowSeconds = true;
        vm.SetTime(testTime);

        // Assert
        vm.FormattedTime.Should().Be("14:30:45");
        vm.AmPm.Should().BeEmpty();
        vm.FormattedDate.Should().Contain("November");
    }

    [Fact]
    public void Display_12HourFormat_IncludesAmPm()
    {
        // Arrange
        using var vm = new ClockWidgetViewModel(startActive: false);
        var testTime = new DateTime(2026, 11, 24, 14, 30, 45);

        // Act
        vm.Is24HourFormat = false;
        vm.ShowSeconds = false;
        vm.SetTime(testTime);

        // Assert
        vm.FormattedTime.Should().Be("02:30");
        vm.AmPm.Should().Be("PM");
    }

    [Fact]
    public void AnalogAngles_CalculatesAnglesAccurately()
    {
        // Arrange: 6:00:00 -> Hour angle 180°, Minute angle 0°, Second angle 0°
        using var vm = new ClockWidgetViewModel(startActive: false);
        var testTime = new DateTime(2026, 11, 24, 6, 0, 0);

        // Act
        vm.SetTime(testTime);

        // Assert
        vm.HourHandAngle.Should().BeApproximately(180.0, 0.1);
        vm.MinuteHandAngle.Should().BeApproximately(0.0, 0.1);
        vm.SecondHandAngle.Should().BeApproximately(0.0, 0.1);
    }

    [Fact]
    public void PauseOnHide_GPU_Law_StopsTickingTimer()
    {
        // Arrange
        using var vm = new ClockWidgetViewModel(startActive: true);
        vm.IsTimerRunning.Should().BeTrue();

        // Act: Launcher hides
        vm.OnLauncherHidden();

        // Assert: Timer stopped immediately
        vm.IsTimerRunning.Should().BeFalse();

        // Act: Launcher shown again
        vm.OnLauncherShown();

        // Assert: Timer resumed
        vm.IsTimerRunning.Should().BeTrue();
    }
}
