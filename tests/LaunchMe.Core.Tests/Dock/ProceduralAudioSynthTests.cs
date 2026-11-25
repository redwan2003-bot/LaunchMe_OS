using FluentAssertions;
using LaunchMe.Contracts.Dock;
using LaunchMe.Core.Dock;
using Xunit;

namespace LaunchMe.Core.Tests.Dock;

public sealed class ProceduralAudioSynthTests
{
    [Theory]
    [InlineData(AmbientSoundPreset.Rain)]
    [InlineData(AmbientSoundPreset.OceanWaves)]
    [InlineData(AmbientSoundPreset.Forest)]
    [InlineData(AmbientSoundPreset.WhiteNoise)]
    [InlineData(AmbientSoundPreset.CafeStream)]
    public void GenerateStereoSamples_ProducesFiniteNonZeroBoundedBuffers_ForAllPresets(AmbientSoundPreset preset)
    {
        var synth = new ProceduralAudioSynth(sampleRate: 48000, seed: 42);
        const int frames = 4096;

        var buffer = synth.GenerateStereoSamples(preset, frames, volume: 0.8);

        buffer.Length.Should().Be(frames * 2);
        buffer.Should().OnlyContain(s => float.IsFinite(s) && s >= -1.0f && s <= 1.0f);

        var rms = Math.Sqrt(buffer.Average(s => (double)s * s));
        rms.Should().BeGreaterThan(0.001, $"preset {preset} should emit audible non-zero energy");

        var bands = ProceduralAudioSynth.ComputeNineBandRms(buffer);
        bands.Length.Should().Be(9);
        bands.Should().OnlyContain(b => float.IsFinite(b) && b >= 0.0f && b <= 1.0f);
        bands.Sum().Should().BeGreaterThan(0.01f);
    }

    [Fact]
    public void GenerateStereoSamples_NonePreset_ProducesSilence()
    {
        var synth = new ProceduralAudioSynth(seed: 7);
        var buffer = synth.GenerateStereoSamples(AmbientSoundPreset.None, 512, volume: 1.0);

        buffer.Should().OnlyContain(s => s == 0f);
        ProceduralAudioSynth.ComputeNineBandRms(buffer).Should().OnlyContain(b => b == 0f);
    }

    [Fact]
    public void OceanWaves_ZeroPointTwelveHzLfo_ModulatesAmplitudeAcrossSwellCycle()
    {
        var synth = new ProceduralAudioSynth(sampleRate: 8000, seed: 123);

        // At 0.12 Hz, period = 8.333s; 1/4 period (crest) is ~2.08s, 3/4 period (trough) is ~6.25s
        var q1 = synth.GenerateStereoSamples(AmbientSoundPreset.OceanWaves, 16000, 1.0); // 0..2s
        var q2 = synth.GenerateStereoSamples(AmbientSoundPreset.OceanWaves, 16000, 1.0); // 2..4s (near crest)
        var q3 = synth.GenerateStereoSamples(AmbientSoundPreset.OceanWaves, 16000, 1.0); // 4..6s
        var q4 = synth.GenerateStereoSamples(AmbientSoundPreset.OceanWaves, 16000, 1.0); // 6..8s (near trough)

        _ = q1;
        _ = q3;
        var crestRms = Math.Sqrt(q2.Average(s => (double)s * s));
        var troughRms = Math.Sqrt(q4.Average(s => (double)s * s));

        crestRms.Should().BeGreaterThan(troughRms * 1.4);
    }

    [Fact]
    public void ComputeNineBandRms_EmptyInput_ReturnsNineZeros()
    {
        var bands = ProceduralAudioSynth.ComputeNineBandRms(ReadOnlySpan<float>.Empty);
        bands.Length.Should().Be(9);
        bands.Should().OnlyContain(b => b == 0f);
    }

    [Fact]
    public void ComputeNineBandRms_PeaksAtMatchingFrequencyBand()
    {
        const int sampleRate = 48000;
        const int frames = 4096;
        var stereo = new float[frames * 2];

        for (var i = 0; i < frames; i++)
        {
            var sample = (float)(0.5 * Math.Sin(2.0 * Math.PI * 1000.0 * i / sampleRate));
            stereo[i * 2] = sample;
            stereo[(i * 2) + 1] = sample;
        }

        var bands = ProceduralAudioSynth.ComputeNineBandRms(stereo, sampleRate);

        bands.Length.Should().Be(9);
        // Band index 4 corresponds to 1000 Hz center frequency
        var peakBandIndex = Array.IndexOf(bands, bands.Max());
        peakBandIndex.Should().Be(4);
    }
}
