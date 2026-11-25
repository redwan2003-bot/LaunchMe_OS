using LaunchMe.Contracts.Dock;

namespace LaunchMe.Core.Dock;

/// <summary>
/// Pure mathematical stereo PCM float sample synthesizer for ambient dock soundscapes
/// and 9-band RMS waveform analyzer.
/// </summary>
public sealed class ProceduralAudioSynth
{
    public const int BandCount = 9;
    private static readonly double[] BandCenterFrequenciesHz = [63, 125, 250, 500, 1000, 2000, 4000, 8000, 14000];

    private readonly int _sampleRate;
    private readonly Random _rng;

    // Paul Kellet pink noise state (Left / Right)
    private double _pinkL0, _pinkL1, _pinkL2, _pinkL3, _pinkL4, _pinkL5, _pinkL6;
    private double _pinkR0, _pinkR1, _pinkR2, _pinkR3, _pinkR4, _pinkR5, _pinkR6;

    // Brown noise / low-pass filter state (Left / Right)
    private double _brownL;
    private double _brownR;
    private double _lpL;
    private double _lpR;
    private double _bpL1, _bpL2;
    private double _bpR1, _bpR2;

    // Phase accumulators
    private double _oceanPhase;
    private double _windPhaseA;
    private double _windPhaseB;
    private double _cafePhase;

    // Active droplet ping (Rain)
    private double _pingPhase;
    private double _pingFreqHz;
    private double _pingEnvelope;
    private double _pingDecay;
    private double _pingPanL;
    private double _pingPanR;

    // Active harmonic chirp (Forest) & ceramic clink (Cafe)
    private double _chirpPhase;
    private double _chirpBaseFreqHz;
    private double _chirpEnvelope;
    private double _chirpDecay;
    private double _chirpPanL;
    private double _chirpPanR;

    public int SampleRate => _sampleRate;

    public ProceduralAudioSynth(int sampleRate = 48000, int? seed = null)
    {
        if (sampleRate < 8000)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be at least 8000 Hz.");
        }

        _sampleRate = sampleRate;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public void Reset()
    {
        _pinkL0 = _pinkL1 = _pinkL2 = _pinkL3 = _pinkL4 = _pinkL5 = _pinkL6 = 0;
        _pinkR0 = _pinkR1 = _pinkR2 = _pinkR3 = _pinkR4 = _pinkR5 = _pinkR6 = 0;
        _brownL = _brownR = _lpL = _lpR = 0;
        _bpL1 = _bpL2 = _bpR1 = _bpR2 = 0;
        _oceanPhase = _windPhaseA = _windPhaseB = _cafePhase = 0;
        _pingPhase = _pingEnvelope = 0;
        _chirpPhase = _chirpEnvelope = 0;
    }

    public float[] GenerateStereoSamples(AmbientSoundPreset preset, int frameCount, double volume = 1.0)
    {
        if (frameCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameCount), "Frame count cannot be negative.");
        }

        var buffer = new float[frameCount * 2];
        GenerateStereoSamples(preset, buffer, volume);
        return buffer;
    }

    public void GenerateStereoSamples(AmbientSoundPreset preset, Span<float> interleavedStereoBuffer, double volume = 1.0)
    {
        if (interleavedStereoBuffer.IsEmpty)
        {
            return;
        }

        if (preset == AmbientSoundPreset.None)
        {
            interleavedStereoBuffer.Clear();
            return;
        }

        var gain = Math.Clamp(volume, 0.0, 1.0);
        var frames = interleavedStereoBuffer.Length / 2;

        for (var i = 0; i < frames; i++)
        {
            var (left, right) = preset switch
            {
                AmbientSoundPreset.Rain => NextRainFrame(),
                AmbientSoundPreset.OceanWaves => NextOceanWaveFrame(),
                AmbientSoundPreset.Forest => NextForestFrame(),
                AmbientSoundPreset.WhiteNoise => NextWhiteNoiseFrame(),
                AmbientSoundPreset.CafeStream => NextCafeStreamFrame(),
                _ => (0.0, 0.0)
            };

            var outL = (float)Math.Clamp(Math.Tanh(left * gain), -1.0, 1.0);
            var outR = (float)Math.Clamp(Math.Tanh(right * gain), -1.0, 1.0);

            var idx = i * 2;
            interleavedStereoBuffer[idx] = float.IsFinite(outL) ? outL : 0f;
            interleavedStereoBuffer[idx + 1] = float.IsFinite(outR) ? outR : 0f;
        }

        // If an odd-length span was provided, zero out the trailing sample cleanly
        if ((interleavedStereoBuffer.Length & 1) != 0)
        {
            interleavedStereoBuffer[^1] = 0f;
        }
    }

    /// <summary>
    /// Computes normalized 9-band RMS energy levels [63 Hz .. 14 kHz] from PCM float samples.
    /// </summary>
    public static float[] ComputeNineBandRms(
        ReadOnlySpan<float> samples,
        int sampleRate = 48000,
        bool isStereoInterleaved = true)
    {
        var bands = new float[BandCount];
        if (samples.IsEmpty || sampleRate < 8000)
        {
            return bands;
        }

        var monoCount = isStereoInterleaved && samples.Length >= 2
            ? samples.Length / 2
            : samples.Length;

        if (monoCount == 0)
        {
            return bands;
        }

        const double q = 1.25;
        Span<double> b0 = stackalloc double[BandCount];
        Span<double> b1 = stackalloc double[BandCount];
        Span<double> b2 = stackalloc double[BandCount];
        Span<double> a1 = stackalloc double[BandCount];
        Span<double> a2 = stackalloc double[BandCount];

        Span<double> x1 = stackalloc double[BandCount];
        Span<double> x2 = stackalloc double[BandCount];
        Span<double> y1 = stackalloc double[BandCount];
        Span<double> y2 = stackalloc double[BandCount];
        Span<double> sumSq = stackalloc double[BandCount];

        var nyquistLimit = sampleRate * 0.45;
        for (var b = 0; b < BandCount; b++)
        {
            var fc = Math.Min(BandCenterFrequenciesHz[b], nyquistLimit);
            var w0 = 2.0 * Math.PI * fc / sampleRate;
            var sinW0 = Math.Sin(w0);
            var cosW0 = Math.Cos(w0);
            var alpha = sinW0 / (2.0 * q);
            var a0 = 1.0 + alpha;

            b0[b] = alpha / a0;
            b1[b] = 0.0;
            b2[b] = -alpha / a0;
            a1[b] = (-2.0 * cosW0) / a0;
            a2[b] = (1.0 - alpha) / a0;
        }

        for (var i = 0; i < monoCount; i++)
        {
            double x = isStereoInterleaved && samples.Length >= 2
                ? 0.5 * (samples[i * 2] + samples[(i * 2) + 1])
                : samples[i];

            if (!double.IsFinite(x))
            {
                x = 0.0;
            }

            for (var b = 0; b < BandCount; b++)
            {
                var y = (b0[b] * x) + (b1[b] * x1[b]) + (b2[b] * x2[b]) - (a1[b] * y1[b]) - (a2[b] * y2[b]);
                x2[b] = x1[b];
                x1[b] = x;
                y2[b] = y1[b];
                y1[b] = y;
                sumSq[b] += y * y;
            }
        }

        for (var b = 0; b < BandCount; b++)
        {
            var rms = Math.Sqrt(sumSq[b] / monoCount);
            bands[b] = double.IsFinite(rms) ? (float)Math.Clamp(rms * 2.5, 0.0, 1.0) : 0f;
        }

        return bands;
    }

    private (double Left, double Right) NextRainFrame()
    {
        var pinkL = NextPinkNoiseLeft() * 0.36;
        var pinkR = NextPinkNoiseRight() * 0.36;

        // Occasional stochastic water droplet pings (~10 drops/sec)
        if (_pingEnvelope < 0.002 && _rng.NextDouble() < (10.0 / _sampleRate))
        {
            _pingFreqHz = 1400.0 + (_rng.NextDouble() * 2100.0);
            _pingEnvelope = 0.28 + (_rng.NextDouble() * 0.16);
            _pingDecay = Math.Exp(-1.0 / (_sampleRate * (0.018 + (_rng.NextDouble() * 0.028))));
            var pan = _rng.NextDouble();
            _pingPanL = Math.Cos(pan * Math.PI * 0.5);
            _pingPanR = Math.Sin(pan * Math.PI * 0.5);
            _pingPhase = 0;
        }

        var ping = 0.0;
        if (_pingEnvelope >= 0.0005)
        {
            ping = Math.Sin(_pingPhase) * _pingEnvelope;
            _pingPhase += (2.0 * Math.PI * _pingFreqHz) / _sampleRate;
            if (_pingPhase > 2.0 * Math.PI) _pingPhase -= 2.0 * Math.PI;
            _pingEnvelope *= _pingDecay;
        }

        return (pinkL + (ping * _pingPanL), pinkR + (ping * _pingPanR));
    }

    private (double Left, double Right) NextOceanWaveFrame()
    {
        // 0.12 Hz LFO swell (~8.33s wave cycle) with subtle stereo phase offset
        var lfoIncrement = (2.0 * Math.PI * 0.12) / _sampleRate;
        _oceanPhase += lfoIncrement;
        if (_oceanPhase >= 2.0 * Math.PI) _oceanPhase -= 2.0 * Math.PI;

        var swellL = 0.22 + (0.78 * Math.Pow(0.5 * (1.0 + Math.Sin(_oceanPhase)), 1.6));
        var swellR = 0.22 + (0.78 * Math.Pow(0.5 * (1.0 + Math.Sin(_oceanPhase + 0.35)), 1.6));

        var rawL = NextPinkNoiseLeft();
        var rawR = NextPinkNoiseRight();

        // Low-pass filter to emulate deep surf + cresting foam
        var alphaL = 0.04 + (0.18 * swellL);
        var alphaR = 0.04 + (0.18 * swellR);
        _lpL += alphaL * (rawL - _lpL);
        _lpR += alphaR * (rawR - _lpR);

        return ((_lpL * swellL * 0.85) + (rawL * swellL * 0.12),
                (_lpR * swellR * 0.85) + (rawR * swellR * 0.12));
    }

    private (double Left, double Right) NextForestFrame()
    {
        // Gentle dual-LFO wind rustle through leaves
        _windPhaseA += (2.0 * Math.PI * 0.09) / _sampleRate;
        _windPhaseB += (2.0 * Math.PI * 0.21) / _sampleRate;
        if (_windPhaseA >= 2.0 * Math.PI) _windPhaseA -= 2.0 * Math.PI;
        if (_windPhaseB >= 2.0 * Math.PI) _windPhaseB -= 2.0 * Math.PI;

        var windMod = 0.35 + (0.30 * Math.Sin(_windPhaseA)) + (0.15 * Math.Sin(_windPhaseB));
        var pinkL = NextPinkNoiseLeft();
        var pinkR = NextPinkNoiseRight();

        _lpL += 0.06 * (pinkL - _lpL);
        _lpR += 0.06 * (pinkR - _lpR);

        // Harmonic birdsong chirps (~1.2 chirps/sec)
        if (_chirpEnvelope < 0.001 && _rng.NextDouble() < (1.4 / _sampleRate))
        {
            _chirpBaseFreqHz = 2100.0 + (_rng.NextDouble() * 1400.0);
            _chirpEnvelope = 0.22 + (_rng.NextDouble() * 0.12);
            _chirpDecay = Math.Exp(-1.0 / (_sampleRate * (0.045 + (_rng.NextDouble() * 0.045))));
            var pan = _rng.NextDouble();
            _chirpPanL = Math.Cos(pan * Math.PI * 0.5);
            _chirpPanR = Math.Sin(pan * Math.PI * 0.5);
            _chirpPhase = 0;
        }

        var chirp = 0.0;
        if (_chirpEnvelope >= 0.0005)
        {
            var instantFreq = _chirpBaseFreqHz * (1.0 + (0.12 * _chirpEnvelope));
            chirp = ((0.75 * Math.Sin(_chirpPhase)) + (0.25 * Math.Sin(2.0 * _chirpPhase))) * _chirpEnvelope;
            _chirpPhase += (2.0 * Math.PI * instantFreq) / _sampleRate;
            if (_chirpPhase > 2.0 * Math.PI) _chirpPhase -= 2.0 * Math.PI;
            _chirpEnvelope *= _chirpDecay;
        }

        var windL = _lpL * windMod * 0.55;
        var windR = _lpR * windMod * 0.55;
        return (windL + (chirp * _chirpPanL), windR + (chirp * _chirpPanR));
    }

    private (double Left, double Right) NextWhiteNoiseFrame()
    {
        var l = NextUniformSigned() * 0.32;
        var r = NextUniformSigned() * 0.32;
        return (l, r);
    }

    private (double Left, double Right) NextCafeStreamFrame()
    {
        // Warm brown room bed + gentle mid-band murmur modulation + occasional soft cup clinks
        var whiteL = NextUniformSigned();
        var whiteR = NextUniformSigned();

        _brownL = (_brownL + (0.022 * whiteL)) / 1.022;
        _brownR = (_brownR + (0.022 * whiteR)) / 1.022;

        _cafePhase += (2.0 * Math.PI * 0.37) / _sampleRate;
        if (_cafePhase >= 2.0 * Math.PI) _cafePhase -= 2.0 * Math.PI;

        var murmurEnv = 0.55 + (0.45 * Math.Sin(_cafePhase));
        var pinkL = NextPinkNoiseLeft();
        var pinkR = NextPinkNoiseRight();

        // Simple resonant mid-band murmur shaping
        _bpL1 += 0.14 * (pinkL - _bpL1);
        _bpL2 += 0.04 * (_bpL1 - _bpL2);
        _bpR1 += 0.14 * (pinkR - _bpR1);
        _bpR2 += 0.04 * (_bpR1 - _bpR2);

        var murmurL = (_bpL1 - _bpL2) * murmurEnv * 0.55;
        var murmurR = (_bpR1 - _bpR2) * murmurEnv * 0.55;

        if (_pingEnvelope < 0.001 && _rng.NextDouble() < (0.8 / _sampleRate))
        {
            _pingFreqHz = 2600.0 + (_rng.NextDouble() * 1600.0);
            _pingEnvelope = 0.14;
            _pingDecay = Math.Exp(-1.0 / (_sampleRate * 0.035));
            var pan = _rng.NextDouble();
            _pingPanL = Math.Cos(pan * Math.PI * 0.5);
            _pingPanR = Math.Sin(pan * Math.PI * 0.5);
            _pingPhase = 0;
        }

        var clink = 0.0;
        if (_pingEnvelope >= 0.0005)
        {
            clink = Math.Sin(_pingPhase) * _pingEnvelope;
            _pingPhase += (2.0 * Math.PI * _pingFreqHz) / _sampleRate;
            if (_pingPhase > 2.0 * Math.PI) _pingPhase -= 2.0 * Math.PI;
            _pingEnvelope *= _pingDecay;
        }

        return ((_brownL * 1.45) + murmurL + (clink * _pingPanL),
                (_brownR * 1.45) + murmurR + (clink * _pingPanR));
    }

    private double NextUniformSigned() => (_rng.NextDouble() * 2.0) - 1.0;

    private double NextPinkNoiseLeft()
    {
        var white = NextUniformSigned();
        _pinkL0 = (0.99886 * _pinkL0) + (white * 0.0555179);
        _pinkL1 = (0.99332 * _pinkL1) + (white * 0.0750759);
        _pinkL2 = (0.96900 * _pinkL2) + (white * 0.1538520);
        _pinkL3 = (0.86650 * _pinkL3) + (white * 0.3104856);
        _pinkL4 = (0.55000 * _pinkL4) + (white * 0.5329522);
        _pinkL5 = (-0.7616 * _pinkL5) - (white * 0.0168980);
        var pink = _pinkL0 + _pinkL1 + _pinkL2 + _pinkL3 + _pinkL4 + _pinkL5 + _pinkL6 + (white * 0.5362);
        _pinkL6 = white * 0.115926;
        return pink * 0.11;
    }

    private double NextPinkNoiseRight()
    {
        var white = NextUniformSigned();
        _pinkR0 = (0.99886 * _pinkR0) + (white * 0.0555179);
        _pinkR1 = (0.99332 * _pinkR1) + (white * 0.0750759);
        _pinkR2 = (0.96900 * _pinkR2) + (white * 0.1538520);
        _pinkR3 = (0.86650 * _pinkR3) + (white * 0.3104856);
        _pinkR4 = (0.55000 * _pinkR4) + (white * 0.5329522);
        _pinkR5 = (-0.7616 * _pinkR5) - (white * 0.0168980);
        var pink = _pinkR0 + _pinkR1 + _pinkR2 + _pinkR3 + _pinkR4 + _pinkR5 + _pinkR6 + (white * 0.5362);
        _pinkR6 = white * 0.115926;
        return pink * 0.11;
    }
}
