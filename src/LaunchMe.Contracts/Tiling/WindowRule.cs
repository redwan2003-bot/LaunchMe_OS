namespace LaunchMe.Contracts.Tiling;

/// <summary>
/// Application-specific matching rule governing floating, sizing, or workspace affinity.
/// </summary>
public record WindowRule(
    string Id,
    string? ProcessPattern,
    string? TitlePattern,
    bool ForceFloat,
    int? AssignedWorkspace,
    WindowRect? InitialSize
);
