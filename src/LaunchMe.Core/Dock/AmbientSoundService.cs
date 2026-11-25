using LaunchMe.Contracts.Dock;

namespace LaunchMe.Core.Dock;

public sealed class AmbientSoundService : IAmbientSoundService
{
    private AmbientSoundPreset _currentPreset = AmbientSoundPreset.None;
    private bool _isPlaying;
    private double _volume = 0.7;
    private readonly object _lock = new();
    private bool _disposed;

    public event EventHandler? StateChanged;

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
            lock (_lock)
            {
                _volume = Math.Clamp(value, 0.0, 1.0);
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Play(AmbientSoundPreset preset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _isPlaying = false;
    }
}
