using LaunchMe.Contracts.Dock;

namespace LaunchMe.Core.Dock;

public sealed class AmbientSoundService : IAmbientSoundService
{
    private readonly IAudioDspEngine? _dspEngine;
    private AmbientSoundPreset _currentPreset = AmbientSoundPreset.None;
    private bool _isPlaying;
    private double _volume = 0.7;
    private readonly object _lock = new();
    private bool _disposed;

    public event EventHandler? StateChanged;
    public event EventHandler<float[]>? WaveformBandsUpdated;

    public AmbientSoundService(IAudioDspEngine? dspEngine = null)
    {
        _dspEngine = dspEngine;
        if (_dspEngine is not null)
        {
            _dspEngine.WaveformBandsUpdated += OnDspWaveformBandsUpdated;
        }
    }

    public AmbientSoundPreset CurrentPreset
    {
        get
        {
            lock (_lock) return _currentPreset;
        }
    }

    public bool IsPlaying
    {
        get
        {
            lock (_lock) return _isPlaying;
        }
    }

    public double Volume
    {
        get
        {
            lock (_lock) return _volume;
        }
        set
        {
            double clamped;
            lock (_lock)
            {
                clamped = Math.Clamp(value, 0.0, 1.0);
                _volume = clamped;
            }

            _dspEngine?.SetVolume(clamped);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Play(AmbientSoundPreset preset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        double volumeSnapshot;
        lock (_lock)
        {
            if (preset == AmbientSoundPreset.None)
            {
                _isPlaying = false;
                _currentPreset = AmbientSoundPreset.None;
            }
            else
            {
                _currentPreset = preset;
                _isPlaying = true;
            }

            volumeSnapshot = _volume;
        }

        if (preset == AmbientSoundPreset.None)
        {
            _dspEngine?.StopPlayback();
        }
        else
        {
            _dspEngine?.StartPreset(preset, volumeSnapshot);
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _isPlaying = false;
        }

        _dspEngine?.StopPlayback();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _isPlaying = false;
            _currentPreset = AmbientSoundPreset.None;
        }

        _dspEngine?.StopPlayback();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDspWaveformBandsUpdated(object? sender, float[] bands)
    {
        if (_disposed) return;
        WaveformBandsUpdated?.Invoke(this, bands);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _isPlaying = false;

        if (_dspEngine is not null)
        {
            _dspEngine.WaveformBandsUpdated -= OnDspWaveformBandsUpdated;
            _dspEngine.StopPlayback();
            _dspEngine.Dispose();
        }
    }
}
