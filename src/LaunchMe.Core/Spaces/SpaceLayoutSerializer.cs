using System.Text.Json;
using LaunchMe.Contracts.Launcher;
using LaunchMe.Contracts.Spaces;

namespace LaunchMe.Core.Spaces;

/// <summary>
/// Snapshot of launcher pages and smart folders extracted from or applied to a space.
/// </summary>
public sealed record LauncherLayoutSnapshot(
    IReadOnlyList<LauncherPage> Pages,
    IReadOnlyList<LauncherFolder> Folders);

/// <summary>
/// Serializes, maps, and persists <see cref="LauncherSettings.Pages"/> and <see cref="LauncherSettings.Folders"/>.
/// </summary>
public static class SpaceLayoutSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string SerializeLayout(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalized = new LauncherSettings
        {
            ClassicView = settings.ClassicView,
            FreeLayout = settings.FreeLayout,
            HideTitles = settings.HideTitles,
            IconScale = settings.IconScale,
            FolderGrid = settings.FolderGrid,
            Pages = NormalizePages(settings.Pages),
            Folders = NormalizeFolders(settings.Folders)
        };

        return JsonSerializer.Serialize(normalized, SerializerOptions);
    }

    public static string SerializeLayout(IEnumerable<LauncherPage> pages, IEnumerable<LauncherFolder> folders)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(folders);

        return SerializeLayout(new LauncherSettings
        {
            Pages = pages.ToList(),
            Folders = folders.ToList()
        });
    }

    public static LauncherSettings DeserializeLayout(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var settings = JsonSerializer.Deserialize<LauncherSettings>(json, SerializerOptions)
            ?? throw new InvalidDataException("Failed to deserialize launcher layout JSON.");

        settings.Pages = NormalizePages(settings.Pages);
        settings.Folders = NormalizeFolders(settings.Folders);
        return settings;
    }

    public static LauncherLayoutSnapshot ExtractLayout(SpaceModel space)
    {
        ArgumentNullException.ThrowIfNull(space);

        var launcher = space.Launcher ?? new LauncherSettings();
        return new LauncherLayoutSnapshot(
            NormalizePages(launcher.Pages),
            NormalizeFolders(launcher.Folders));
    }

    public static void ApplyLayout(
        SpaceModel space,
        IEnumerable<LauncherPage> pages,
        IEnumerable<LauncherFolder> folders)
    {
        ArgumentNullException.ThrowIfNull(space);
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(folders);

        space.Launcher ??= new LauncherSettings();
        space.Launcher.Pages = NormalizePages(pages);
        space.Launcher.Folders = NormalizeFolders(folders);
    }

    public static async Task SaveLayoutAsync(
        ISpaceStore spaceStore,
        SpaceModel space,
        IEnumerable<LauncherPage> pages,
        IEnumerable<LauncherFolder> folders,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(spaceStore);
        ArgumentNullException.ThrowIfNull(space);

        ApplyLayout(space, pages, folders);
        await spaceStore.SaveSpaceAsync(space, ct).ConfigureAwait(false);
    }

    public static async Task<SpaceModel> SaveActiveLayoutAsync(
        ISpaceStore spaceStore,
        IEnumerable<LauncherPage> pages,
        IEnumerable<LauncherFolder> folders,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(spaceStore);
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(folders);

        var activeSpace = await spaceStore.GetActiveSpaceAsync(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No active space is available to save launcher layout.");

        await SaveLayoutAsync(spaceStore, activeSpace, pages, folders, ct).ConfigureAwait(false);
        return activeSpace;
    }

    private static List<LauncherPage> NormalizePages(IEnumerable<LauncherPage>? pages)
    {
        if (pages == null) return [];

        return pages
            .Where(static p => p != null)
            .OrderBy(static p => p.PageIndex)
            .Select(static p => new LauncherPage
            {
                PageIndex = p.PageIndex,
                Items = CloneItems(p.Items)
            })
            .ToList();
    }

    private static List<LauncherFolder> NormalizeFolders(IEnumerable<LauncherFolder>? folders)
    {
        if (folders == null) return [];

        return folders
            .Where(static f => f != null)
            .Select(static f => new LauncherFolder
            {
                Id = f.Id,
                Title = f.Title,
                ColorHex = f.ColorHex,
                Emoji = f.Emoji,
                IconPath = f.IconPath,
                GridColumns = f.GridColumns,
                GridRows = f.GridRows,
                IsEmbeddedFolder = f.IsEmbeddedFolder,
                FolderPath = f.FolderPath,
                Items = CloneItems(f.Items)
            })
            .ToList();
    }

    private static List<LauncherItem> CloneItems(IEnumerable<LauncherItem>? items)
    {
        if (items == null) return [];

        return items
            .Where(static i => i != null)
            .Select(static i => new LauncherItem
            {
                Id = i.Id,
                Title = i.Title,
                ItemType = i.ItemType,
                GridRow = i.GridRow,
                GridColumn = i.GridColumn,
                AppId = i.AppId,
                TargetPayload = i.TargetPayload,
                CustomIconPath = i.CustomIconPath,
                FolderId = i.FolderId
            })
            .ToList();
    }
}
