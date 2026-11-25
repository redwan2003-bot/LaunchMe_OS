using System.Text.Json;
using LaunchMe.Contracts.Notch;

namespace LaunchMe.Core.Notch;

public sealed class PocketService : IPocketService
{
    private readonly string _storagePath;
    private readonly List<PocketItem> _items = [];
    private readonly object _lock = new();
    private bool _disposed;

    public event EventHandler? ItemsChanged;

    public PocketService(string? storagePath = null)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _storagePath = Path.Combine(localAppData, "LaunchMe", "pocket.json");
        }
        else
        {
            _storagePath = storagePath;
        }

        LoadFromDisk();
    }

    public IReadOnlyList<PocketItem> GetItems()
    {
        lock (_lock)
        {
            return _items.ToList();
        }
    }

    public PocketItem AddFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ObjectDisposedException.ThrowIf(_disposed, this);

        string fileName = Path.GetFileName(filePath);
        long sizeBytes = 0;
        try
        {
            if (File.Exists(filePath))
            {
                var fi = new FileInfo(filePath);
                sizeBytes = fi.Length;
            }
        }
        catch
        {
            // Non-critical file access failure, size remains 0
        }

        var item = new PocketItem(
            Id: Guid.NewGuid().ToString(),
            FilePath: filePath,
            FileName: string.IsNullOrEmpty(fileName) ? filePath : fileName,
            FileSizeBytes: sizeBytes,
            AddedAt: DateTime.UtcNow);

        lock (_lock)
        {
            _items.Add(item);
            SaveToDisk();
        }

        ItemsChanged?.Invoke(this, EventArgs.Empty);
        return item;
    }

    public bool RemoveFile(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ObjectDisposedException.ThrowIf(_disposed, this);

        bool removed;
        lock (_lock)
        {
            int index = _items.FindIndex(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _items.RemoveAt(index);
                SaveToDisk();
                removed = true;
            }
            else
            {
                removed = false;
            }
        }

        if (removed)
        {
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        return removed;
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _items.Clear();
            SaveToDisk();
        }

        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadFromDisk()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    string json = File.ReadAllText(_storagePath);
                    var loaded = JsonSerializer.Deserialize<List<PocketItem>>(json);
                    if (loaded != null)
                    {
                        _items.Clear();
                        _items.AddRange(loaded);
                    }
                }
            }
            catch
            {
                // Graceful fallback to empty shelf on corrupt file
            }
        }
    }

    private void SaveToDisk()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storagePath, json);
        }
        catch
        {
            // Non-fatal persistence error
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
