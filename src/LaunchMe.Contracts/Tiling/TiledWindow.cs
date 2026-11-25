namespace LaunchMe.Contracts.Tiling;

/// <summary>
/// Represents a tracked desktop window managed by the tiling engine.
/// </summary>
public record TiledWindow(
    nint Handle,
    string ProcessName,
    string Title,
    WindowRect Frame,
    bool IsFloating,
    bool IsPinnedToScratchpad,
    int WorkspaceId
);
