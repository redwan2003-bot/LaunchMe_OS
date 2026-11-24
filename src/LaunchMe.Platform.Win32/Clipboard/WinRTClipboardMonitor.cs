using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using LaunchMe.Contracts.Clipboard;

namespace LaunchMe.Platform.Win32.Clipboard;

public sealed class WinRTClipboardMonitor : IClipboardMonitor
{
    private readonly ClipboardStore _store = new();
    private readonly object _syncLock = new();
    private bool _isListening;
    private bool _disposed;

    public bool IsRunning => _isListening;

    public event EventHandler<ClipboardEntry>? ItemCaptured;

    public void Start()
    {
        lock (_syncLock)
        {
            if (_isListening || _disposed)
            {
                return;
            }

            Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged += OnClipboardContentChanged;
            _isListening = true;
        }
    }

    public void Stop()
    {
        lock (_syncLock)
        {
            if (!_isListening)
            {
                return;
            }

            Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged -= OnClipboardContentChanged;
            _isListening = false;
        }
    }

    private async void OnClipboardContentChanged(object? sender, object e)
    {
        try
        {
            var dataPackage = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (dataPackage == null)
            {
                return;
            }

            string text = string.Empty;
            var filePaths = new List<string>();
            bool hasBitmap = dataPackage.Contains(StandardDataFormats.Bitmap);

            if (dataPackage.Contains(StandardDataFormats.Text))
            {
                text = await dataPackage.GetTextAsync();
                if (text != null && text.Length > 100_000)
                {
                    // Memory safety: Truncate massive clipboard payloads to 100k chars to prevent memory exhaustion
                    text = text.Substring(0, 100_000);
                }
            }

            if (dataPackage.Contains(StandardDataFormats.StorageItems))
            {
                var items = await dataPackage.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (item is StorageFile file)
                    {
                        filePaths.Add(file.Path);
                    }
                    else if (item is StorageFolder folder)
                    {
                        filePaths.Add(folder.Path);
                    }
                }
            }

            var category = ClipboardClassifier.Classify(text, filePaths, hasBitmap);

            var entry = new ClipboardEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Category = category,
                TextContent = text,
                FilePaths = filePaths,
                CapturedAtUtc = DateTime.UtcNow,
                IsPinned = false
            };

            _store.AddEntry(entry);
            ItemCaptured?.Invoke(this, entry);
        }
        catch
        {
            // Clipboard access may fail if locked by another process
        }
    }

    public IReadOnlyList<ClipboardEntry> GetItems(ClipboardCategory category) => _store.GetItems(category);

    public void PinFavorite(string itemId) => _store.PinFavorite(itemId);

    public void UnpinFavorite(string itemId) => _store.UnpinFavorite(itemId);

    public void DeleteItem(string itemId) => _store.DeleteItem(itemId);

    public void ClearCategory(ClipboardCategory category) => _store.ClearCategory(category);

    public void ReorderCategories(IReadOnlyList<ClipboardCategory> order) => _store.ReorderCategories(order);

    public IReadOnlyList<ClipboardCategory> GetCategoryOrder() => _store.GetCategoryOrder();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }
}
