using FluentAssertions;
using LaunchMe.Contracts.Dock;
using LaunchMe.Platform.Win32.Media;
using Xunit;

namespace LaunchMe.Platform.Tests.Media;

public class WinRTAudioDspEngineTests
{
    [Fact]
    public void ImplementsIAudioDspEngine()
    {
        using IAudioDspEngine engine = new WinRTAudioDspEngine();
        engine.IsActive.Should().BeFalse();
    }

    [Fact]
    public void StartStopVolumeAndDispose_AreSafeHeadless()
    {
        using var engine = new WinRTAudioDspEngine();

        engine.SetVolume(0.5);
        engine.SetVolume(-1.0);
        engine.SetVolume(2.5);
        engine.SetVolume(double.NaN);

        foreach (AmbientSoundPreset preset in Enum.GetValues<AmbientSoundPreset>())
        {
            engine.StartPreset(preset, 0.4);
            if (preset == AmbientSoundPreset.None)
            {
                engine.IsActive.Should().BeFalse();
            }
        }

        // Enforce Pause-on-Hide / StopPlayback(): immediately stops and clears IsActive.
        engine.StopPlayback();
        engine.IsActive.Should().BeFalse();

        // Idempotent StopPlayback and Dispose must never throw.
        engine.StopPlayback();
        engine.Dispose();
        engine.StopPlayback();
        engine.Dispose();
    }

    [Fact]
    public void StartPreset_AfterDispose_ThrowsObjectDisposedException()
    {
        var engine = new WinRTAudioDspEngine();
        engine.Dispose();

        Action act = () => engine.StartPreset(AmbientSoundPreset.Rain, 0.5);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Theory]
    [InlineData(AmbientSoundPreset.Rain)]
    [InlineData(AmbientSoundPreset.OceanWaves)]
    [InlineData(AmbientSoundPreset.Forest)]
    [InlineData(AmbientSoundPreset.WhiteNoise)]
    [InlineData(AmbientSoundPreset.CafeStream)]
    public void SynthesizeQuantum_ProducesBoundedStereoPcmAndNineBandRms(AmbientSoundPreset preset)
    {
        using var engine = new WinRTAudioDspEngine();
        const int frames = 480; // 10 ms at 48 kHz
        var stereo = new float[frames * WinRTAudioDspEngine.ChannelCount];
        var bands = new float[WinRTAudioDspEngine.BandCount];

        engine.SynthesizeQuantum(preset, 0.75f, stereo, bands);

        stereo.Should().OnlyContain(sample => sample >= -1f && sample <= 1f && !float.IsNaN(sample));
        stereo.Any(sample => MathF.Abs(sample) > 1e-6f).Should().BeTrue();

        bands.Should().HaveCount(WinRTAudioDspEngine.BandCount);
        bands.Should().OnlyContain(b => b >= 0f && b <= 1f && !float.IsNaN(b));
        bands.Any(b => b > 0f).Should().BeTrue();
    }

    [Fact]
    public void SynthesizeQuantum_NoneOrZeroVolume_ProducesSilenceAndZeroBands()
    {
        using var engine = new WinRTAudioDspEngine();
        var stereo = new float[480 * WinRTAudioDspEngine.ChannelCount];
        var bands = new float[WinRTAudioDspEngine.BandCount];

        engine.SynthesizeQuantum(AmbientSoundPreset.None, 0.8f, stereo, bands);
        stereo.Should().OnlyContain(s => s == 0f);
        bands.Should().OnlyContain(b => b == 0f);

        engine.SynthesizeQuantum(AmbientSoundPreset.WhiteNoise, 0f, stereo, bands);
        stereo.Should().OnlyContain(s => s == 0f);
        bands.Should().OnlyContain(b => b == 0f);
    }
}
