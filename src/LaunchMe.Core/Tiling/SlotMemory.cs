using LaunchMe.Contracts.Tiling;

namespace LaunchMe.Core.Tiling;

/// <summary>
/// Records the layout slot vacated by a window so it can be restored seamlessly.
/// </summary>
public record SlotRecord(
    nint Hwnd,
    nint PartnerHwnd,
    SplitDirection Side,
    SplitAxis Axis,
    double Ratio
);

/// <summary>
/// Manages remembered window slots for minimized or temporarily hidden windows.
/// </summary>
public class SlotMemory
{
    private readonly Dictionary<nint, SlotRecord> _memory = new();

    public void Remember(nint hwnd, nint partnerHwnd, SplitDirection side, SplitAxis axis, double ratio)
    {
        _memory[hwnd] = new SlotRecord(hwnd, partnerHwnd, side, axis, ratio);
    }

    public SlotRecord? Recall(nint hwnd)
    {
        return _memory.TryGetValue(hwnd, out var record) ? record : null;
    }

    public void Forget(nint hwnd)
    {
        _memory.Remove(hwnd);
    }

    public void Clear()
    {
        _memory.Clear();
    }
}
