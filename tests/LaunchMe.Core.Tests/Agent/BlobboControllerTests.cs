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

    [Fact]
    public void SmoothPupilStep_InterpolatesWithDefaultAlpha()
    {
        double next = BlobboController.SmoothPupilStep(0.0, 4.0);

        next.Should().BeApproximately(1.0, 1e-6);
    }

    [Theory]
    [InlineData(-0.5, 0.04)]
    [InlineData(0.0, 0.04)]
    [InlineData(0.5, 2.0)]
    [InlineData(1.0, 4.0)]
    [InlineData(2.5, 4.0)]
    public void SmoothPupilStep_ClampsAlphaBetweenPoint01AndOne(double alpha, double expected)
    {
        double next = BlobboController.SmoothPupilStep(0.0, 4.0, alpha);

        next.Should().BeApproximately(expected, 1e-6);
    }

    [Fact]
    public void SmoothPupilStep_NaNAlphaFallsBackToDefault()
    {
        double next = BlobboController.SmoothPupilStep(0.0, 4.0, double.NaN);

        next.Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void UpdatePupilsSmooth_ApproachesTargetGraduallyAndStaysBounded()
    {
        var blobbo = new BlobboController();

        blobbo.UpdatePupilsSmooth(cursorX: 1920, cursorY: 10, snootCenterX: 960, snootCenterY: 10, alpha: 0.25);
        double step1 = blobbo.PupilOffset.PupilX;

        blobbo.UpdatePupilsSmooth(cursorX: 1920, cursorY: 10, snootCenterX: 960, snootCenterY: 10, alpha: 0.25);
        double step2 = blobbo.PupilOffset.PupilX;

        step1.Should().BeApproximately(1.25, 1e-6);
        step2.Should().BeApproximately(2.1875, 1e-6);
        step2.Should().BeGreaterThan(step1);
        step2.Should().BeLessThanOrEqualTo(5.0);
        blobbo.PupilOffset.PupilY.Should().BeApproximately(0.0, 1e-6);
    }

    [Fact]
    public void UpdatePupilsSmooth_ResetsImmediatelyWhenSleepingOrDizzy()
    {
        var blobbo = new BlobboController();
        blobbo.UpdatePupilsSmooth(cursorX: 1920, cursorY: 500, snootCenterX: 960, snootCenterY: 10, alpha: 1.0);
        blobbo.PupilOffset.PupilX.Should().BeGreaterThan(0);

        blobbo.SetMood(BlobboMood.Sleeping);
        blobbo.UpdatePupilsSmooth(cursorX: 1920, cursorY: 500, snootCenterX: 960, snootCenterY: 10, alpha: 0.25);
        blobbo.PupilOffset.Should().Be((0, 0));

        blobbo.SetMood(BlobboMood.Idle);
        blobbo.UpdatePupilsSmooth(cursorX: 1920, cursorY: 500, snootCenterX: 960, snootCenterY: 10, alpha: 1.0);
        blobbo.SetMood(BlobboMood.Dizzy);
        blobbo.UpdatePupilsSmooth(cursorX: 1920, cursorY: 500, snootCenterX: 960, snootCenterY: 10, alpha: 0.25);
        blobbo.PupilOffset.Should().Be((0, 0));
    }
}
