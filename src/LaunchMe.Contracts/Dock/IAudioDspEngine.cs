namespace LaunchMe.Contracts.Dock;

/// <summary>
/// Native Windows AudioGraph procedural DSP synthesizer and 9-band RMS waveform emitter for the Floating Capsule Dock.
/// </summary>
public interface IAudioDspEngine : IDisposable
{
    bool IsActive { get; }
    void StartPreset(AmbientSoundPreset preset, double volume);
    void StopPlayback();
    void SetVolume(double volume);
    event EventHandler<float[]>? WaveformBandsUpdated;
}
