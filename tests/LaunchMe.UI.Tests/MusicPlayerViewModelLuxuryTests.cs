using FluentAssertions;
using LaunchMe.Contracts.Media;
using LaunchMe.UI.ViewModels;
using Microsoft.UI.Xaml;
using Xunit;

namespace LaunchMe.UI.Tests;

public sealed class MusicPlayerViewModelLuxuryTests : IDisposable
{
    private readonly MusicPlayerViewModel _vm = new();

    public void Dispose() => _vm.Dispose();

    [Fact]
    public void VisualizerModes_SwitchVisibilitiesCleanly()
    {
        _vm.SelectFullArtModeCommand.Execute(null);
        _vm.VisualizerMode.Should().Be(MusicVisualizerMode.FullScreenArt);
        _vm.FullScreenArtModeVisibility.Should().Be(Visibility.Visible);
        _vm.VinylModeVisibility.Should().Be(Visibility.Collapsed);

        _vm.SelectParticlesModeCommand.Execute(null);
        _vm.ParticlesModeVisibility.Should().Be(Visibility.Visible);
        _vm.ParticleNodes.Should().HaveCount(18);

        _vm.SelectBeatsModeCommand.Execute(null);
        _vm.GlowingBeatsModeVisibility.Should().Be(Visibility.Visible);
        _vm.BeatBars.Should().HaveCount(16);
    }

    [Fact]
    public void StepVisualizerFrame_AnimatesWhenPlayingAndFreezesOnHide()
    {
        _vm.IsPlaying = true;
        double initialAngle = _vm.VinylAngle;
        double initialParticleX = _vm.ParticleNodes[0].X;
        double initialBarHeight = _vm.BeatBars[0].HeightPx;

        _vm.StepVisualizerFrame(0.05);
        _vm.VinylAngle.Should().NotBe(initialAngle);
        _vm.ParticleNodes[0].X.Should().NotBe(initialParticleX);
        _vm.BeatBars[0].HeightPx.Should().NotBe(initialBarHeight);

        // Pause-on-Hide GPU Law
        _vm.OnHidden();
        double frozenAngle = _vm.VinylAngle;
        _vm.StepVisualizerFrame(0.05);
        _vm.VinylAngle.Should().Be(frozenAngle);

        _vm.OnShown();
        _vm.StepVisualizerFrame(0.05);
        _vm.VinylAngle.Should().NotBe(frozenAngle);
    }

    [Fact]
    public void LayoutAndWallpaperSync_ToggleAndEmitTrackEvent()
    {
        MediaTrackInfo? syncedTrack = null;
        _vm.MusicArtWallpaperSyncRequested += (_, t) => syncedTrack = t;

        _vm.ToggleLayoutCommand.Execute(null);
        _vm.IsVerticalLayout.Should().BeFalse();
        _vm.LayoutLabel.Should().Be("Widescreen");

        _vm.ToggleWallpaperSyncCommand.Execute(null);
        _vm.SyncWallpaperToArt.Should().BeTrue();

        var track = new MediaTrackInfo { Title = "Midnight City", Artist = "M83", IsPlaying = true };
        _vm.UpdateFromTrack(track);
        syncedTrack.Should().Be(track);
    }
}
