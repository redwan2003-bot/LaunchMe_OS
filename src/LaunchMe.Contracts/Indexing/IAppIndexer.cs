namespace LaunchMe.Contracts.Indexing;

/// <summary>
/// Scans Start Menu (.lnk), MSIX packages, and custom extra sources with 128px icon atlas caching.
/// </summary>
public interface IAppIndexer
{
    IReadOnlyList<AppItem> AllApps { get; }
    Task ScanAllAsync(CancellationToken ct = default);
    void AddExtraSource(string folderPath);
    void RemoveExtraSource(string folderPath);
    IReadOnlyList<string> GetExtraSources();
    IReadOnlyList<AppItem> SearchApps(string query);
    Task<bool> CleanUninstallAsync(string appId, bool moveLeftoversToRecycleBin, CancellationToken ct = default);
    void HideApp(string appId, bool isHidden);
    void RenameApp(string appId, string customDisplayName);

    event EventHandler? IndexingCompleted;
}
