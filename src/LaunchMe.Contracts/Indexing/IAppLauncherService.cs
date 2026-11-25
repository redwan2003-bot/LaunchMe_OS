namespace LaunchMe.Contracts.Indexing;

/// <summary>
/// Launches indexed Win32 shortcuts, portable executables, and packaged MSIX applications.
/// </summary>
public interface IAppLauncherService
{
    Task<bool> LaunchAppAsync(AppItem app, CancellationToken ct = default);
    bool LaunchTarget(string targetPath, string? arguments = null);
}
