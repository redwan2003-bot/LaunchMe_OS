using FluentAssertions;
using LaunchMe.Contracts.Agent;
using LaunchMe.Core.Agent;
using Xunit;

namespace LaunchMe.Core.Tests.Agent;

public class BlobboControllerTests
{
    [Fact]
    public void InitialMood_IsIdle()
    {
        var blobbo = new BlobboController();

        blobbo.CurrentMood.Should().Be(BlobboMood.Idle);
        blobbo.PupilOffset.Should().Be((0, 0));
    }

    [Fact]
    public void SetMood_UpdatesMoodAndFiresEvent()
    {
        var blobbo = new BlobboController();
        BlobboMood? firedMood = null;
        blobbo.MoodChanged += (_, m) => firedMood = m;

        blobbo.SetMood(BlobboMood.Thinking);

        blobbo.CurrentMood.Should().Be(BlobboMood.Thinking);
        firedMood.Should().Be(BlobboMood.Thinking);
    }

    [Fact]
    public void UpdatePupils_ClampsOffsetWithinMaxRadius()
    {
        var blobbo = new BlobboController();

        blobbo.UpdatePupils(cursorX: 1920, cursorY: 500, snootCenterX: 960, snootCenterY: 10);

        blobbo.PupilOffset.PupilX.Should().BeGreaterThan(0);
        blobbo.PupilOffset.PupilX.Should().BeLessThanOrEqualTo(5.0);
    }

    [Fact]
    public void Poke_Once_SetsPokedMood()
    {
        var blobbo = new BlobboController();
        blobbo.Poke();

        blobbo.CurrentMood.Should().Be(BlobboMood.Poked);
    }

    [Fact]
    public void Poke_ThreeTimesQuickly_SetsDizzyMood()
    {
        var blobbo = new BlobboController();
        blobbo.Poke();
        blobbo.Poke();
        blobbo.Poke();

        blobbo.CurrentMood.Should().Be(BlobboMood.Dizzy);
    }
}
