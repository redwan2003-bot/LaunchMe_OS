namespace LaunchMe.Contracts.Triggers;

public enum TriggerType
{
    Hotkey,
    HotCorner,
    TouchpadPinch,
    TouchpadSpread,
    NotchClick,
    DockItem
}

public enum HotCornerLocation
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public sealed class TriggerEventArgs : EventArgs
{
    public TriggerType TriggerType { get; }
    public HotCornerLocation? Corner { get; }
    public bool Handled { get; set; }
    public int? HotkeyId { get; init; }
    public uint? VirtualKey { get; init; }
    public uint? Modifiers { get; init; }

    public TriggerEventArgs(TriggerType triggerType, HotCornerLocation? corner = null)
    {
        TriggerType = triggerType;
        Corner = corner;
    }
}
