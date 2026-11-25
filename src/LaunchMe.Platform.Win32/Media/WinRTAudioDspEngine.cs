using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using LaunchMe.Contracts.Dock;
using WinRT;

namespace LaunchMe.Platform.Win32.Media;

/// <summary>
/// Native Windows AudioGraph procedural DSP synthesizer and 9-band RMS waveform emitter
/// for the Floating Capsule Dock ambient soundscape player.
/// </summary>
public sealed class WinRTAudioDspEngine : IAudioDspEngine
{
    public const int SampleRate = 48000;
    public const int ChannelCount = 2;
    public const int BandCount = 9;

    // Logarithmically spaced center frequencies across audible spectrum (63 Hz to 12 kHz).
    private static readonly float[] BandCenterFrequencies =
    [
        63f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 12000f
    ];

    private readonly object _lifecycleLock = new();
    private readonly BiquadBandFilter[] _bandFilters = CreateBandFilters();

    private AudioGraph? _audioGraph;
    private AudioFrameInputNode? _frameInputNode;
    private AudioDeviceOutputNode? _deviceOutputNode;

    private AmbientSoundPreset _activePreset = AmbientSoundPreset.None;
    private double _volume = 0.7;
    private volatile bool _isActive;
    private bool _disposed;

    // Procedural DSP state (accessed exclusively from the audio quantum thread or under lock when stopped).
    private uint _rngStateL = 0x12345678u;
    private uint _rngStateR = 0x87654321u;
    private float _brownL;
    private float _brownR;
    private float _pinkL0, _pinkL1, _pinkL2;
    private float _pinkR0, _pinkR1, _pinkR2;
    private double _lfoPhase;
    private double _harmonicPhase;
    private float _dropletEnv;
    private float _birdEnv;
    private float _birdFreq = 2400f;
    private int _samplesUntilEvent = 4800;

    public bool IsActive => _isActive;

    public event EventHandler<float[]>? WaveformBandsUpdated;

    public void StartPreset(AmbientSoundPreset preset, double volume)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        double clampedVolume = ClampVolume(volume);

        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            Volatile.Write(ref _volume, clampedVolume);

            if (preset == AmbientSoundPreset.None)
            {
                StopPlaybackLocked();
                return;
            }

            _activePreset = preset;

            if (_audioGraph is not null && _frameInputNode is not null)
            {
                if (!_isActive)
                {
                    try
                    {
                        _audioGraph.Start();
                        _isActive = true;
                    }
                    catch
                    {
                        StopPlaybackLocked();
                    }
                }

                return;
            }

            // Initialize AudioGraph off the caller's sync context to prevent WinRT async deadlocks.
            try
            {
                Task<bool> initTask = Task.Run(InitializeAndStartGraphAsync);
                bool completed = initTask.Wait(TimeSpan.FromSeconds(3));
                if (!completed || !initTask.Result)
                {
                    StopPlaybackLocked();
                }
            }
            catch
            {
                // Degrade cleanly in headless CI or environments without audio output endpoints.
                StopPlaybackLocked();
            }
        }
    }

    public void StopPlayback()
    {
        lock (_lifecycleLock)
        {
            StopPlaybackLocked();
        }
    }

    public void SetVolume(double volume)
    {
        Volatile.Write(ref _volume, ClampVolume(volume));
    }

    public void Dispose()
    {
        lock (_lifecycleLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopPlaybackLocked();
        }
    }

    private async Task<bool> InitializeAndStartGraphAsync()
    {
        AudioGraph? graph = null;
        AudioDeviceOutputNode? outputNode = null;
        AudioFrameInputNode? inputNode = null;

        try
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Media);
            var graphResult = await AudioGraph.CreateAsync(settings).AsTask().ConfigureAwait(false);
            if (graphResult.Status != AudioGraphCreationStatus.Success || graphResult.Graph is null)
            {
                return false;
            }

            graph = graphResult.Graph;

            var outputResult = await graph.CreateDeviceOutputNodeAsync().AsTask().ConfigureAwait(false);
            if (outputResult.Status != AudioDeviceNodeCreationStatus.Success || outputResult.DeviceOutputNode is null)
            {
                graph.Dispose();
                return false;
            }

            outputNode = outputResult.DeviceOutputNode;

            var encoding = AudioEncodingProperties.CreatePcm(SampleRate, ChannelCount, 32);
            encoding.Subtype = MediaEncodingSubtypes.Float;

            inputNode = graph.CreateFrameInputNode(encoding);
            inputNode.AddOutgoingConnection(outputNode);
            inputNode.QuantumStarted += OnQuantumStarted;

            graph.Start();

            _audioGraph = graph;
            _deviceOutputNode = outputNode;
            _frameInputNode = inputNode;
            _isActive = true;
            return true;
        }
        catch
        {
            if (inputNode is not null)
            {
                try
                {
                    inputNode.QuantumStarted -= OnQuantumStarted;
                    inputNode.Dispose();
                }
                catch
                {
                }
            }

            if (outputNode is not null)
            {
                try
                {
                    outputNode.Dispose();
                }
                catch
                {
                }
            }

            if (graph is not null)
            {
                try
                {
                    graph.Dispose();
                }
                catch
                {
                }
            }

            _isActive = false;
            return false;
        }
    }

    private void StopPlaybackLocked()
    {
        // Enforce Pause-on-Hide immediately before tearing down nodes.
        _isActive = false;
        _activePreset = AmbientSoundPreset.None;

        if (_frameInputNode is not null)
        {
            try
            {
                _frameInputNode.QuantumStarted -= OnQuantumStarted;
                _frameInputNode.Stop();
                _frameInputNode.Dispose();
            }
            catch
            {
            }

            _frameInputNode = null;
        }

        if (_deviceOutputNode is not null)
        {
            try
            {
                _deviceOutputNode.Dispose();
            }
            catch
            {
            }

            _deviceOutputNode = null;
        }

        if (_audioGraph is not null)
        {
            try
            {
                _audioGraph.Stop();
                _audioGraph.Dispose();
            }
            catch
            {
            }

            _audioGraph = null;
        }

        ResetDspState();
    }

    private unsafe void OnQuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
    {
        if (!_isActive || _disposed)
        {
            return;
        }

        int requiredSamples = args.RequiredSamples;
        if (requiredSamples <= 0)
        {
            return;
        }

        uint bufferCapacityBytes = (uint)(requiredSamples * ChannelCount * sizeof(float));
        using var frame = new AudioFrame(bufferCapacityBytes);
        var bands = new float[BandCount];

        using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
        using (var reference = buffer.CreateReference())
        {
            var byteAccess = reference.As<IMemoryBufferByteAccess>();
            byteAccess.GetBuffer(out byte* dataPtr, out uint capacity);

            int floatCount = (int)Math.Min(capacity / sizeof(float), (uint)(requiredSamples * ChannelCount));
            var interleavedStereo = new Span<float>(dataPtr, floatCount);

            SynthesizeQuantum(_activePreset, (float)Volatile.Read(ref _volume), interleavedStereo, bands);
        }

        if (!_isActive || _disposed)
        {
            return;
        }

        try
        {
            sender.AddFrame(frame);
        }
        catch
        {
            return;
        }

        try
        {
            WaveformBandsUpdated?.Invoke(this, bands);
        }
        catch
        {
        }
    }

    /// <summary>
    /// Synthesizes 48 kHz interleaved stereo float PCM samples for the specified preset
    /// and computes 9-band normalized RMS envelope levels in [0, 1].
    /// </summary>
    public void SynthesizeQuantum(
        AmbientSoundPreset preset,
        float volume,
        Span<float> interleavedStereo,
        Span<float> bandsOut)
    {
        int frameCount = interleavedStereo.Length / ChannelCount;
        if (frameCount <= 0 || preset == AmbientSoundPreset.None)
        {
            interleavedStereo.Clear();
            bandsOut.Clear();
            return;
        }

        Span<double> bandEnergy = stackalloc double[BandCount];
        float gain = Math.Clamp(volume, 0f, 1f);

        for (int i = 0; i < frameCount; i++)
        {
            GenerateStereoSample(preset, out float left, out float right);

            left = Math.Clamp(left * gain, -1f, 1f);
            right = Math.Clamp(right * gain, -1f, 1f);

            int sampleIndex = i * ChannelCount;
            interleavedStereo[sampleIndex] = left;
            interleavedStereo[sampleIndex + 1] = right;

            float mono = 0.5f * (left + right);
            for (int b = 0; b < BandCount; b++)
            {
                float filtered = _bandFilters[b].Process(mono);
                bandEnergy[b] += filtered * filtered;
            }
        }

        int outCount = Math.Min(BandCount, bandsOut.Length);
        for (int b = 0; b < outCount; b++)
        {
            float rms = MathF.Sqrt((float)(bandEnergy[b] / frameCount));
            // Scale gentle ambient RMS into an expressive [0, 1] UI envelope range.
            bandsOut[b] = Math.Clamp(rms * 3.5f, 0f, 1f);
        }
    }

    private void GenerateStereoSample(AmbientSoundPreset preset, out float left, out float right)
    {
        float whiteL = NextUniformBipolar(ref _rngStateL);
        float whiteR = NextUniformBipolar(ref _rngStateR);

        switch (preset)
        {
            case AmbientSoundPreset.WhiteNoise:
                left = whiteL * 0.25f;
                right = whiteR * 0.25f;
                break;

            case AmbientSoundPreset.Rain:
            {
                float pinkL = StepPinkFilter(whiteL, ref _pinkL0, ref _pinkL1, ref _pinkL2);
                float pinkR = StepPinkFilter(whiteR, ref _pinkR0, ref _pinkR1, ref _pinkR2);

                if (--_samplesUntilEvent <= 0)
                {
                    _dropletEnv = 0.35f;
                    _samplesUntilEvent = 800 + (int)(NextUniformUnipolar(ref _rngStateL) * 5200f);
                }

                _dropletEnv *= 0.9965f;
                float drop = whiteL * _dropletEnv * 0.55f;
                left = pinkL * 0.24f + drop;
                right = pinkR * 0.24f + (whiteR * _dropletEnv * 0.45f);
                break;
            }

            case AmbientSoundPreset.OceanWaves:
            {
                _lfoPhase = (_lfoPhase + (0.12 / SampleRate)) % 1.0;
                float swellL = 0.35f + 0.65f * (0.5f + 0.5f * MathF.Sin((float)(_lfoPhase * Math.Tau)));
                float swellR = 0.35f + 0.65f * (0.5f + 0.5f * MathF.Sin((float)((_lfoPhase + 0.08) * Math.Tau)));

                _brownL = (_brownL + whiteL * 0.03f) * 0.992f;
                _brownR = (_brownR + whiteR * 0.03f) * 0.992f;

                float surfL = _brownL * 1.4f + whiteL * 0.06f * swellL;
                float surfR = _brownR * 1.4f + whiteR * 0.06f * swellR;
                left = surfL * swellL;
                right = surfR * swellR;
                break;
            }

            case AmbientSoundPreset.Forest:
            {
                _lfoPhase = (_lfoPhase + (0.08 / SampleRate)) % 1.0;
                float breeze = 0.4f + 0.6f * (0.5f + 0.5f * MathF.Sin((float)(_lfoPhase * Math.Tau)));
                _brownL = (_brownL + whiteL * 0.018f) * 0.994f;
                _brownR = (_brownR + whiteR * 0.018f) * 0.994f;

                if (--_samplesUntilEvent <= 0)
                {
                    _birdEnv = 0.14f;
                    _birdFreq = 2100f + NextUniformUnipolar(ref _rngStateR) * 1400f;
                    _samplesUntilEvent = 14000 + (int)(NextUniformUnipolar(ref _rngStateL) * 32000f);
                }

                _birdEnv *= 0.9993f;
                _harmonicPhase = (_harmonicPhase + (_birdFreq / SampleRate)) % 1.0;
                float chirp = MathF.Sin((float)(_harmonicPhase * Math.Tau)) * _birdEnv;

                left = _brownL * breeze * 1.3f + chirp * 0.7f;
                right = _brownR * breeze * 1.3f + chirp * 0.5f;
                break;
            }

            case AmbientSoundPreset.CafeStream:
            {
                _lfoPhase = (_lfoPhase + (0.35 / SampleRate)) % 1.0;
                float chatterMod = 0.65f + 0.35f * MathF.Sin((float)(_lfoPhase * Math.Tau));
                _brownL = (_brownL + whiteL * 0.025f) * 0.985f;
                _brownR = (_brownR + whiteR * 0.025f) * 0.985f;

                float pinkL = StepPinkFilter(whiteL, ref _pinkL0, ref _pinkL1, ref _pinkL2);
                float pinkR = StepPinkFilter(whiteR, ref _pinkR0, ref _pinkR1, ref _pinkR2);

                if (--_samplesUntilEvent <= 0)
                {
                    _birdEnv = 0.08f;
                    _birdFreq = 1650f + NextUniformUnipolar(ref _rngStateL) * 900f;
                    _samplesUntilEvent = 24000 + (int)(NextUniformUnipolar(ref _rngStateR) * 48000f);
                }

                _birdEnv *= 0.9990f;
                _harmonicPhase = (_harmonicPhase + (_birdFreq / SampleRate)) % 1.0;
                float clink = MathF.Sin((float)(_harmonicPhase * Math.Tau)) * _birdEnv;

                left = (_brownL * 1.1f + pinkL * 0.12f) * chatterMod + clink * 0.4f;
                right = (_brownR * 1.1f + pinkR * 0.12f) * chatterMod + clink * 0.6f;
                break;
            }

            default:
                left = 0f;
                right = 0f;
                break;
        }
    }

    private void ResetDspState()
    {
        _brownL = 0f;
        _brownR = 0f;
        _pinkL0 = _pinkL1 = _pinkL2 = 0f;
        _pinkR0 = _pinkR1 = _pinkR2 = 0f;
        _lfoPhase = 0.0;
        _harmonicPhase = 0.0;
        _dropletEnv = 0f;
        _birdEnv = 0f;
        _samplesUntilEvent = 4800;

        foreach (var filter in _bandFilters)
        {
            filter.Reset();
        }
    }

    private static double ClampVolume(double volume)
    {
        if (double.IsNaN(volume))
        {
            return 0.0;
        }

        return Math.Clamp(volume, 0.0, 1.0);
    }

    private static float StepPinkFilter(float white, ref float b0, ref float b1, ref float b2)
    {
        b0 = 0.99765f * b0 + white * 0.0990460f;
        b1 = 0.96300f * b1 + white * 0.2965164f;
        b2 = 0.57000f * b2 + white * 1.0526913f;
        return (b0 + b1 + b2 + white * 0.1848f) * 0.25f;
    }

    private static float NextUniformUnipolar(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0x00FFFFFFu) / 16777216f;
    }

    private static float NextUniformBipolar(ref uint state)
    {
        return NextUniformUnipolar(ref state) * 2f - 1f;
    }

    private static BiquadBandFilter[] CreateBandFilters()
    {
        var filters = new BiquadBandFilter[BandCount];
        for (int i = 0; i < BandCount; i++)
        {
            filters[i] = new BiquadBandFilter(BandCenterFrequencies[i], SampleRate, q: 1.4f);
        }

        return filters;
    }

    private sealed class BiquadBandFilter
    {
        private readonly float _b0;
        private readonly float _b1;
        private readonly float _b2;
        private readonly float _a1;
        private readonly float _a2;

        private float _x1;
        private float _x2;
        private float _y1;
        private float _y2;

        public BiquadBandFilter(float centerFreq, float sampleRate, float q)
        {
            float omega = MathF.Tau * centerFreq / sampleRate;
            float sinOmega = MathF.Sin(omega);
            float cosOmega = MathF.Cos(omega);
            float alpha = sinOmega / (2f * q);
            float a0 = 1f + alpha;

            _b0 = alpha / a0;
            _b1 = 0f;
            _b2 = -alpha / a0;
            _a1 = (-2f * cosOmega) / a0;
            _a2 = (1f - alpha) / a0;
        }

        public float Process(float x)
        {
            float y = (_b0 * x) + (_b1 * _x1) + (_b2 * _x2) - (_a1 * _y1) - (_a2 * _y2);
            _x2 = _x1;
            _x1 = x;
            _y2 = _y1;
            _y1 = y;
            return y;
        }

        public void Reset()
        {
            _x1 = _x2 = _y1 = _y2 = 0f;
        }
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
