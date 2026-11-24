namespace LaunchMe.Contracts.Spaces;

/// <summary>
/// Persistence and lifecycle manager for Spaces and portable .launchmetheme packages.
/// </summary>
public interface ISpaceStore
{
    Task<IReadOnlyList<SpaceModel>> GetAllSpacesAsync(CancellationToken ct = default);
    Task<SpaceModel?> GetActiveSpaceAsync(CancellationToken ct = default);
    Task SwitchSpaceAsync(string spaceId, CancellationToken ct = default);
    Task SaveSpaceAsync(SpaceModel space, CancellationToken ct = default);
    Task DeleteSpaceAsync(string spaceId, CancellationToken ct = default);
    Task ExportThemeAsync(string spaceId, string outputZipPath, CancellationToken ct = default);
    Task<SpaceModel> ImportThemeAsync(string themeZipPath, CancellationToken ct = default);

    event EventHandler<SpaceChangedEventArgs>? SpaceChanged;
}
