namespace LaunchMe.Contracts.Dock;

public enum AmbientSoundPreset
{
    None,
    Rain,
    OceanWaves,
    Forest,
    WhiteNoise,
    CafeStream
}

public interface IAmbientSoundService : IDisposable
{
    AmbientSoundPreset CurrentPreset { get; }
    bool IsPlaying { get; }
    double Volume { get; set; }
    void Play(AmbientSoundPreset preset);
    void Pause();
    void Stop();
    event EventHandler? StateChanged;
}
