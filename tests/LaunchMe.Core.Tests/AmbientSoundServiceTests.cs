using LaunchMe.Contracts.Dock;
using LaunchMe.Core.Dock;
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
}
