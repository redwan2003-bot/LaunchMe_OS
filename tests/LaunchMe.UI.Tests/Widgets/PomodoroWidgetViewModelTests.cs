using FluentAssertions;
using LaunchMe.UI.Widgets.Pomodoro;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

public sealed class PomodoroWidgetViewModelTests
{
    [Fact]
    public void InitialState_IsWork25Minutes()
    {
        // Arrange & Act
        using var vm = new PomodoroWidgetViewModel();

        // Assert
        vm.CurrentState.Should().Be(PomodoroState.Work);
        vm.RemainingSeconds.Should().Be(25 * 60);
        vm.RemainingTimeFormatted.Should().Be("25:00");
        vm.IsRunning.Should().BeFalse();
        vm.CompletedCycles.Should().Be(0);
    }

    [Fact]
    public void TransitionToNextState_SwitchesFromWorkToShortBreak()
    {
        // Arrange
        using var vm = new PomodoroWidgetViewModel();
        bool audioFired = false;
        vm.AudioCueRequested += (_, _) => audioFired = true;

        // Act: Work session finishes
        vm.TransitionToNextState();

        // Assert: Advances to Short Break (5 min)
        vm.CurrentState.Should().Be(PomodoroState.ShortBreak);
        vm.RemainingSeconds.Should().Be(5 * 60);
        vm.RemainingTimeFormatted.Should().Be("05:00");
        vm.CompletedCycles.Should().Be(1);
        audioFired.Should().BeTrue();
    }

    [Fact]
    public void FourthCycle_TriggersLongBreak()
    {
        // Arrange
        using var vm = new PomodoroWidgetViewModel();

        // Act: Complete 3 full work/break cycles, then finish 4th work cycle
        // 1st cycle: Work -> ShortBreak
        vm.TransitionToNextState();
        // ShortBreak -> Work
        vm.TransitionToNextState();

        // 2nd cycle: Work -> ShortBreak
        vm.TransitionToNextState();
        // ShortBreak -> Work
        vm.TransitionToNextState();

        // 3rd cycle: Work -> ShortBreak
        vm.TransitionToNextState();
        // ShortBreak -> Work
        vm.TransitionToNextState();

        // 4th cycle: Work completes -> Long Break
        vm.TransitionToNextState();

        // Assert: 4th work cycle completed triggers Long Break (15 min)
        vm.CurrentState.Should().Be(PomodoroState.LongBreak);
        vm.RemainingSeconds.Should().Be(15 * 60);
        vm.RemainingTimeFormatted.Should().Be("15:00");
        vm.CompletedCycles.Should().Be(4);
    }

    [Fact]
    public void StartPauseReset_TogglesRunningState()
    {
        // Arrange
        using var vm = new PomodoroWidgetViewModel();

        // Act: Start
        vm.ToggleStartPause();
        vm.IsRunning.Should().BeTrue();

        // Act: Pause
        vm.ToggleStartPause();
        vm.IsRunning.Should().BeFalse();

        // Act: Reset
        vm.RemainingSeconds = 120;
        vm.Reset();
        vm.RemainingSeconds.Should().Be(25 * 60);
    }
}
