namespace LaunchMe.Contracts.Notch;

/// <summary>
/// Represents a temporary multi-file shelf item in the Dynamic Notch Pocket.
/// </summary>
public sealed record PocketItem(
    string Id,
    string FilePath,
    string FileName,
    long FileSizeBytes,
    DateTime AddedAt);

/// <summary>
/// Service managing temporary Pocket shelf items.
/// </summary>
public interface IPocketService : IDisposable
{
    IReadOnlyList<PocketItem> GetItems();
    PocketItem AddFile(string filePath);
    bool RemoveFile(string id);
    void Clear();
    event EventHandler? ItemsChanged;
}
