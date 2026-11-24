namespace LaunchMe.Contracts.Clipboard;

/// <summary>
/// Monitors system clipboard changes and maintains categorized, reorderable history.
/// </summary>
public interface IClipboardMonitor : IDisposable
{
    bool IsRunning { get; }
    void Start();
    void Stop();
    IReadOnlyList<ClipboardEntry> GetItems(ClipboardCategory category);
    void PinFavorite(string itemId);
    void UnpinFavorite(string itemId);
    void DeleteItem(string itemId);
    void ClearCategory(ClipboardCategory category);
    void ReorderCategories(IReadOnlyList<ClipboardCategory> order);
    IReadOnlyList<ClipboardCategory> GetCategoryOrder();

    event EventHandler<ClipboardEntry>? ItemCaptured;
}
