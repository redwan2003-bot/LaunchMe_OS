namespace LaunchMe.Contracts.Triggers;

/// <summary>
/// Coordinates system-wide triggers: global hotkeys, low-level mouse hook Hot Corners, and Precision Touchpad pinches.
/// </summary>
public interface IGestureAndTriggerService : IDisposable
{
    void RegisterGlobalHotkey(int modifiers, int virtualKey);
    void UnregisterGlobalHotkey();
    void SetHotCorners(HotCornerConfig config);
    void EnableTouchpadPinch(int fingerCount, bool enabled);

    event EventHandler<TriggerEventArgs>? Invoked;
}
