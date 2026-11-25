using FluentAssertions;
using LaunchMe.Contracts.Agent;
using LaunchMe.Core.Agent;
using Xunit;

namespace LaunchMe.Core.Tests.Agent;

public class MochiControllerTests
{
    [Fact]
    public void InitialMood_IsIdle()
    {
        var mochi = new MochiController();

        mochi.CurrentMood.Should().Be(MochiMood.Idle);
        mochi.PupilOffset.Should().Be((0, 0));
    }

    [Fact]
    public void SetMood_UpdatesMoodAndFiresEvent()
    {
        var mochi = new MochiController();
        MochiMood? firedMood = null;
        mochi.MoodChanged += (_, m) => firedMood = m;

        mochi.SetMood(MochiMood.Thinking);

        mochi.CurrentMood.Should().Be(MochiMood.Thinking);
        firedMood.Should().Be(MochiMood.Thinking);
    }

    [Fact]
    public void UpdatePupils_ClampsOffsetWithinMaxRadius()
    {
        var mochi = new MochiController();

        // Cursor way off to the right
        mochi.UpdatePupils(cursorX: 1920, cursorY: 500, notchCenterX: 960, notchCenterY: 10);

        mochi.PupilOffset.PupilX.Should().BeGreaterThan(0);
        mochi.PupilOffset.PupilX.Should().BeLessThanOrEqualTo(5.0);
    }

    [Fact]
    public void Poke_Once_SetsPokedMood()
    {
        var mochi = new MochiController();
        mochi.Poke();

        mochi.CurrentMood.Should().Be(MochiMood.Poked);
    }

    [Fact]
    public void Poke_ThreeTimesQuickly_SetsDizzyMood()
    {
        var mochi = new MochiController();
        mochi.Poke();
        mochi.Poke();
        mochi.Poke();

        mochi.CurrentMood.Should().Be(MochiMood.Dizzy);
    }
}
