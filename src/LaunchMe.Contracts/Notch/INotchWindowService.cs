namespace LaunchMe.Contracts.Notch;

/// <summary>
/// Controls window affinity exclusion and fullscreen game detection for the Dynamic Notch.
/// </summary>
public interface INotchWindowService : IDisposable
{
    void ApplyScreenCaptureExclusion(nint hwnd, bool exclude);
    bool IsFullscreenGameActive();
}
