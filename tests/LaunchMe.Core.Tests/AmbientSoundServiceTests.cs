using FluentAssertions;
using LaunchMe.Contracts.Dock;
using LaunchMe.Core.Dock;
using Moq;
using Xunit;

namespace LaunchMe.Core.Tests;

public sealed class AmbientSoundServiceTests
{
    [Fact]
    public void Play_SetsPresetAndPlayingState()
    {
        using var service = new AmbientSoundService();
        bool changed = false;
        service.StateChanged += (_, _) => changed = true;

        service.Play(AmbientSoundPreset.Rain);

        Assert.True(service.IsPlaying);
        Assert.Equal(AmbientSoundPreset.Rain, service.CurrentPreset);
        Assert.True(changed);
    }

    [Fact]
    public void PauseAndStop_UpdatesStateCorrectly()
    {
        using var service = new AmbientSoundService();
        service.Play(AmbientSoundPreset.OceanWaves);
        Assert.True(service.IsPlaying);

        service.Pause();
        Assert.False(service.IsPlaying);
        Assert.Equal(AmbientSoundPreset.OceanWaves, service.CurrentPreset);

        service.Stop();
        Assert.False(service.IsPlaying);
        Assert.Equal(AmbientSoundPreset.None, service.CurrentPreset);
    }

    [Fact]
    public void Volume_ClampedBetweenZeroAndOne()
    {
        using var service = new AmbientSoundService();

        service.Volume = 1.5;
        Assert.Equal(1.0, service.Volume);

        service.Volume = -0.5;
        Assert.Equal(0.0, service.Volume);

        service.Volume = 0.65;
        Assert.Equal(0.65, service.Volume);
    }

    [Fact]
    public void DspEngine_ReceivesForwardedPlayPauseStopVolumeAndWaveformEvents()
    {
        var mockDsp = new Mock<IAudioDspEngine>();
        var service = new AmbientSoundService(mockDsp.Object);

        float[]? receivedBands = null;
        service.WaveformBandsUpdated += (_, bands) => receivedBands = bands;

        service.Volume = 1.4;
        mockDsp.Verify(x => x.SetVolume(1.0), Times.Once);

        service.Play(AmbientSoundPreset.Forest);
        mockDsp.Verify(x => x.StartPreset(AmbientSoundPreset.Forest, 1.0), Times.Once);

        var expectedBands = new[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };
        mockDsp.Raise(x => x.WaveformBandsUpdated += null, mockDsp.Object, expectedBands);
        receivedBands.Should().BeSameAs(expectedBands);

        service.Pause();
        mockDsp.Verify(x => x.StopPlayback(), Times.Once);

        service.Play(AmbientSoundPreset.None);
        mockDsp.Verify(x => x.StopPlayback(), Times.Exactly(2));

        service.Stop();
        mockDsp.Verify(x => x.StopPlayback(), Times.Exactly(3));

        service.Dispose();
        mockDsp.Verify(x => x.Dispose(), Times.Once);
    }
}
