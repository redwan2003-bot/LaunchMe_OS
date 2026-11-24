using LaunchMe.Contracts.Clipboard;

namespace LaunchMe.Platform.Win32.Clipboard;

public class ClipboardStore
{
    private readonly object _lock = new();
    private readonly List<ClipboardEntry> _items = [];
    private readonly List<ClipboardCategory> _categoryOrder =
    [
        ClipboardCategory.Recent,
        ClipboardCategory.Favorites,
        ClipboardCategory.Text,
        ClipboardCategory.Code,
        ClipboardCategory.Images,
        ClipboardCategory.Screenshots,
        ClipboardCategory.Colors,
        ClipboardCategory.Files
    ];

    private readonly int _maxCapacity;

    public ClipboardStore(int maxCapacity = 500)
    {
        _maxCapacity = maxCapacity;
    }

    public void AddEntry(ClipboardEntry entry)
    {
        lock (_lock)
        {
            if (_items.Count > 0)
            {
                var top = _items[0];
                if (!string.IsNullOrEmpty(entry.TextContent) && top.TextContent == entry.TextContent)
                {
                    top.CapturedAtUtc = DateTime.UtcNow;
                    return;
                }
            }

            _items.Insert(0, entry);
            if (_items.Count > _maxCapacity)
            {
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    if (!_items[i].IsPinned)
                    {
                        _items.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }

    public IReadOnlyList<ClipboardEntry> GetItems(ClipboardCategory category)
    {
        lock (_lock)
        {
            return category switch
            {
                ClipboardCategory.Recent => _items.OrderByDescending(x => x.CapturedAtUtc).ToList(),
                ClipboardCategory.Favorites => _items.Where(x => x.IsPinned).OrderByDescending(x => x.CapturedAtUtc).ToList(),
                _ => _items.Where(x => x.Category == category).OrderByDescending(x => x.CapturedAtUtc).ToList()
            };
        }
    }

    public void PinFavorite(string itemId)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(x => x.Id == itemId);
            if (item != null)
            {
                item.IsPinned = true;
            }
        }
    }

    public void UnpinFavorite(string itemId)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(x => x.Id == itemId);
            if (item != null)
            {
                item.IsPinned = false;
            }
        }
    }

    public void DeleteItem(string itemId)
    {
        lock (_lock)
        {
            _items.RemoveAll(x => x.Id == itemId);
        }
    }

    public void ClearCategory(ClipboardCategory category)
    {
        lock (_lock)
        {
            if (category == ClipboardCategory.Favorites)
            {
                foreach (var item in _items)
                {
                    item.IsPinned = false;
                }
            }
            else if (category == ClipboardCategory.Recent)
            {
                _items.RemoveAll(x => !x.IsPinned);
            }
            else
            {
                _items.RemoveAll(x => x.Category == category && !x.IsPinned);
            }
        }
    }

    public void ReorderCategories(IReadOnlyList<ClipboardCategory> order)
    {
        lock (_lock)
        {
            _categoryOrder.Clear();
            _categoryOrder.AddRange(order);
        }
    }

    public IReadOnlyList<ClipboardCategory> GetCategoryOrder()
    {
        lock (_lock)
        {
            return _categoryOrder.ToList();
        }
    }
}
