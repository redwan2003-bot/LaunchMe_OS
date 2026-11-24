using FluentAssertions;
using LaunchMe.Core.Widgets.Pomodoro;
using Xunit;

namespace LaunchMe.Core.Tests.Widgets;

public sealed class PomodoroStateMachineTests
{
    [Fact]
    public void InitialState_IsIdle()
    {
        var sm = new PomodoroStateMachine();
        sm.CurrentState.Should().Be(PomodoroState.Idle);
        sm.RemainingTime.Should().Be(TimeSpan.Zero);
        sm.CompletedWorkSessions.Should().Be(0);
    }

    [Fact]
    public void Start_TransitionsToWork_With25MinutesDefaultDuration()
    {
        var sm = new PomodoroStateMachine();
        AudioCueEventArgs? cueArgs = null;
        sm.AudioCueTriggered += (_, e) => cueArgs = e;

        sm.Start();

        sm.CurrentState.Should().Be(PomodoroState.Work);
        sm.RemainingTime.Should().Be(TimeSpan.FromMinutes(25));
        sm.TotalDuration.Should().Be(TimeSpan.FromMinutes(25));
        sm.Progress.Should().Be(0.0);

        cueArgs.Should().NotBeNull();
        cueArgs!.CueType.Should().Be(AudioCueType.WorkStarted);
    }

    [Fact]
    public void Pause_And_Resume_FunctionsCorrectly()
    {
        var sm = new PomodoroStateMachine();
        sm.Start();
        sm.Tick(TimeSpan.FromMinutes(5));

        sm.Pause();
        sm.CurrentState.Should().Be(PomodoroState.Paused);
        sm.RemainingTime.Should().Be(TimeSpan.FromMinutes(20));

        // Ticking while paused should do nothing
        sm.Tick(TimeSpan.FromMinutes(5));
        sm.RemainingTime.Should().Be(TimeSpan.FromMinutes(20));

        sm.Resume();
        sm.CurrentState.Should().Be(PomodoroState.Work);
        sm.RemainingTime.Should().Be(TimeSpan.FromMinutes(20));
    }

    [Fact]
    public void Tick_TriggersHalfwayCue_And_CountdownCue()
    {
        var sm = new PomodoroStateMachine(new PomodoroConfig
        {
            WorkDuration = TimeSpan.FromSeconds(20)
        });

        var cues = new List<AudioCueType>();
        sm.AudioCueTriggered += (_, e) => cues.Add(e.CueType);

        sm.Start(); // WorkStarted
        sm.Tick(TimeSpan.FromSeconds(10)); // Halfway (10s remaining of 20s)
        sm.Tick(TimeSpan.FromSeconds(6));  // 4s remaining -> Countdown tick

        cues.Should().Contain(AudioCueType.HalfwayProgress);
        cues.Should().Contain(AudioCueType.FinalCountdownTick);
    }

    [Fact]
    public void CompletingWorkSession_IncrementsCount_And_TransitionsToBreak()
    {
        var config = new PomodoroConfig
        {
            WorkDuration = TimeSpan.FromMinutes(25),
            ShortBreakDuration = TimeSpan.FromMinutes(5),
            AutoStartBreaks = true,
            SessionsUntilLongBreak = 4
        };
        var sm = new PomodoroStateMachine(config);

        sm.Start();
        sm.Tick(TimeSpan.FromMinutes(25)); // Completes session

        sm.CompletedWorkSessions.Should().Be(1);
        sm.CurrentState.Should().Be(PomodoroState.ShortBreak);
        sm.RemainingTime.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void FourthCompletedSession_TransitionsToLongBreak()
    {
        var config = new PomodoroConfig
        {
            WorkDuration = TimeSpan.FromMinutes(25),
            ShortBreakDuration = TimeSpan.FromMinutes(5),
            LongBreakDuration = TimeSpan.FromMinutes(15),
            AutoStartBreaks = true,
            AutoStartWork = true,
            SessionsUntilLongBreak = 4
        };
        var sm = new PomodoroStateMachine(config);

        for (var i = 0; i < 3; i++)
        {
            sm.Start();
            sm.Tick(TimeSpan.FromMinutes(25)); // Work complete -> ShortBreak
            sm.Tick(TimeSpan.FromMinutes(5));  // Break complete -> Work
        }

        // 4th work session
        sm.CurrentState.Should().Be(PomodoroState.Work);
        sm.Tick(TimeSpan.FromMinutes(25));

        sm.CompletedWorkSessions.Should().Be(4);
        sm.CurrentState.Should().Be(PomodoroState.LongBreak);
        sm.RemainingTime.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void Reset_RestoresIdleState()
    {
        var sm = new PomodoroStateMachine();
        sm.Start();
        sm.Tick(TimeSpan.FromMinutes(10));

        sm.Reset(resetCompletedSessions: true);

        sm.CurrentState.Should().Be(PomodoroState.Idle);
        sm.RemainingTime.Should().Be(TimeSpan.Zero);
        sm.CompletedWorkSessions.Should().Be(0);
    }
}
