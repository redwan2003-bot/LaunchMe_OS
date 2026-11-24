using LaunchMe.Contracts.Triggers;

namespace LaunchMe.Platform.Win32.Gestures;

public sealed class GestureAndTriggerService : IGestureAndTriggerService
{
    private readonly Win32HotkeyService _hotkeyService;
    private readonly Win32HotCornerService _hotCornerService;
    private bool _disposed;

    public event EventHandler<TriggerEventArgs>? Invoked;

    public GestureAndTriggerService()
    {
        _hotkeyService = new Win32HotkeyService();
        _hotCornerService = new Win32HotCornerService();

        _hotkeyService.HotkeyTriggered += OnServiceTriggered;
        _hotCornerService.CornerTriggered += OnServiceTriggered;
        _hotCornerService.Start();
    }

    public void RegisterGlobalHotkey(int modifiers, int virtualKey)
    {
        _hotkeyService.Register((uint)modifiers, (uint)virtualKey);
    }

    public void UnregisterGlobalHotkey()
    {
        _hotkeyService.UnregisterAll();
    }

    public void UnregisterGlobalHotkey(int modifiers, int virtualKey)
    {
        _hotkeyService.Unregister((uint)modifiers, (uint)virtualKey);
    }

    public void SetHotCorners(HotCornerConfig config)
    {
        _hotCornerService.SetConfig(config);
    }

    public void EnableTouchpadPinch(int fingerCount, bool enabled)
    {
        // Touchpad pinch handler
    }

    private void OnServiceTriggered(object? sender, TriggerEventArgs e)
    {
        Invoked?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _hotkeyService.HotkeyTriggered -= OnServiceTriggered;
        _hotCornerService.CornerTriggered -= OnServiceTriggered;

        _hotkeyService.Dispose();
        _hotCornerService.Dispose();
    }
}
