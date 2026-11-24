using System.Collections.Concurrent;
using LaunchMe.Contracts.Indexing;
using Microsoft.VisualBasic.FileIO;
using Windows.Management.Deployment;

namespace LaunchMe.Core.Apps;

public sealed class AppIndexer : IAppIndexer
{
    private readonly IconCacheService _iconCache;
    private readonly ConcurrentBag<string> _extraSources = [];
    private readonly ConcurrentDictionary<string, AppItem> _indexedApps = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, bool> _hiddenOverrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _renamedOverrides = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? IndexingCompleted;

    public IReadOnlyList<AppItem> AllApps => _indexedApps.Values.ToList();

    public AppIndexer(IconCacheService? iconCache = null)
    {
        _iconCache = iconCache ?? new IconCacheService();
    }

    public void AddExtraSource(string folderPath)
    {
        if (Directory.Exists(folderPath) && !_extraSources.Contains(folderPath, StringComparer.OrdinalIgnoreCase))
        {
            _extraSources.Add(folderPath);
        }
    }

    public void RemoveExtraSource(string folderPath)
    {
        var remaining = _extraSources.Where(s => !s.Equals(folderPath, StringComparison.OrdinalIgnoreCase)).ToList();
        _extraSources.Clear();
        foreach (var item in remaining)
        {
            _extraSources.Add(item);
        }
    }

    public IReadOnlyList<string> GetExtraSources() => _extraSources.ToList();

    public async Task ScanAllAsync(CancellationToken ct = default)
    {
        _indexedApps.Clear();

        // 1. Scan Common & User Start Menu Programs
        var startMenuFolders = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs")
        };

        foreach (var dir in startMenuFolders.Where(Directory.Exists))
        {
            ScanDirectoryShortcuts(dir, ct);
        }

        // 2. Scan Extra User Sources
        foreach (var extra in _extraSources.Where(Directory.Exists))
        {
            ScanDirectoryShortcuts(extra, ct);
            ScanExecutables(extra, ct);
        }

        // 3. Scan MSIX / UWP Packages
        await ScanUwpPackagesAsync(ct).ConfigureAwait(false);

        // Apply overrides
        foreach (var app in _indexedApps.Values)
        {
            if (_hiddenOverrides.TryGetValue(app.Id, out var isHidden))
            {
                app.IsHidden = isHidden;
            }
            if (_renamedOverrides.TryGetValue(app.Id, out var renamed))
            {
                app.CustomDisplayName = renamed;
            }
        }

        IndexingCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void ScanDirectoryShortcuts(string rootDir, CancellationToken ct)
    {
        try
        {
            var files = Directory.EnumerateFiles(rootDir, "*.lnk", System.IO.SearchOption.AllDirectories);
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                var target = ShellLinkHelper.ResolveShortcutTarget(file);
                if (string.IsNullOrWhiteSpace(target) || !File.Exists(target)) continue;

                var fileName = Path.GetFileNameWithoutExtension(file);
                var appId = $"lnk:{file.ToLowerInvariant()}";
                var iconPath = _iconCache.GetOrExtractIcon(appId, target);
                var base64 = _iconCache.ConvertIconToBase64(iconPath);

                var item = new AppItem
                {
                    Id = appId,
                    Name = fileName,
                    ExecutablePath = target,
                    IconCachePath = iconPath,
                    IconBase64 = base64,
                    AppType = AppType.Win32Shortcut,
                    Categories = [ClassifyCategory(fileName, target)]
                };

                _indexedApps[appId] = item;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* Ignore inaccessible folders */ }
    }

    private void ScanExecutables(string rootDir, CancellationToken ct)
    {
        try
        {
            var exes = Directory.EnumerateFiles(rootDir, "*.exe", System.IO.SearchOption.AllDirectories);
            foreach (var exe in exes)
            {
                ct.ThrowIfCancellationRequested();
                var fileName = Path.GetFileNameWithoutExtension(exe);
                var appId = $"exe:{exe.ToLowerInvariant()}";

                var iconPath = _iconCache.GetOrExtractIcon(appId, exe);
                var base64 = _iconCache.ConvertIconToBase64(iconPath);

                var item = new AppItem
                {
                    Id = appId,
                    Name = fileName,
                    ExecutablePath = exe,
                    IconCachePath = iconPath,
                    IconBase64 = base64,
                    AppType = AppType.ExtraSource,
                    Categories = [ClassifyCategory(fileName, exe)]
                };

                _indexedApps[appId] = item;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* Ignore inaccessible folders */ }
    }

    private Task ScanUwpPackagesAsync(CancellationToken ct)
    {
        try
        {
            var packageManager = new PackageManager();
            var packages = packageManager.FindPackagesForUser(string.Empty);

            foreach (var package in packages)
            {
                ct.ThrowIfCancellationRequested();
                if (package.IsFramework || package.IsResourcePackage) continue;

                var id = package.Id;
                if (string.IsNullOrWhiteSpace(id?.FamilyName)) continue;

                var appId = $"uwp:{id.FamilyName.ToLowerInvariant()}";
                var displayName = package.DisplayName;
                if (string.IsNullOrWhiteSpace(displayName)) displayName = id.Name;

                var item = new AppItem
                {
                    Id = appId,
                    Name = displayName,
                    ExecutablePath = package.InstalledLocation?.Path ?? string.Empty,
                    AppType = AppType.MsixPackage,
                    Categories = [ClassifyCategory(displayName, id.Name)]
                };

                _indexedApps.TryAdd(appId, item);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            // Non-packaged / test runtime fallback
        }

        return Task.CompletedTask;
    }

    private static string ClassifyCategory(string name, string targetPath)
    {
        var combined = $"{name} {targetPath}".ToLowerInvariant();

        if (combined.Contains("code") || combined.Contains("studio") || combined.Contains("terminal") || combined.Contains("git") || combined.Contains("rider"))
            return "Development";

        if (combined.Contains("chrome") || combined.Contains("edge") || combined.Contains("firefox") || combined.Contains("browser") || combined.Contains("opera"))
            return "Browsers";

        if (combined.Contains("spotify") || combined.Contains("music") || combined.Contains("vlc") || combined.Contains("player") || combined.Contains("netflix"))
            return "Media";

        if (combined.Contains("word") || combined.Contains("excel") || combined.Contains("powerpoint") || combined.Contains("notion") || combined.Contains("office") || combined.Contains("notes"))
            return "Productivity";

        if (combined.Contains("game") || combined.Contains("steam") || combined.Contains("epic") || combined.Contains("xbox"))
            return "Games";

        if (combined.Contains("photoshop") || combined.Contains("figma") || combined.Contains("blender") || combined.Contains("canva"))
            return "Design";

        if (combined.Contains("calc") || combined.Contains("cmd") || combined.Contains("powershell") || combined.Contains("control") || combined.Contains("settings"))
            return "System";

        return "General";
    }

    public IReadOnlyList<AppItem> SearchApps(string query)
    {
        var apps = _indexedApps.Values.AsEnumerable();

        if (string.IsNullOrWhiteSpace(query))
        {
            return apps.OrderBy(a => a.DisplayName).ToList();
        }

        var q = query.Trim();
        return apps
            .Select(a => new
            {
                App = a,
                Score = CalculateScore(a.DisplayName, q)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.App.DisplayName)
            .Select(x => x.App)
            .ToList();
    }

    private static double CalculateScore(string text, string query)
    {
        if (text.Equals(query, StringComparison.OrdinalIgnoreCase)) return 1.0;
        if (text.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 0.85;
        if (text.Contains(query, StringComparison.OrdinalIgnoreCase)) return 0.65;
        return 0.0;
    }

    public void HideApp(string appId, bool isHidden)
    {
        _hiddenOverrides[appId] = isHidden;
        if (_indexedApps.TryGetValue(appId, out var app))
        {
            app.IsHidden = isHidden;
        }
    }

    public void RenameApp(string appId, string customDisplayName)
    {
        if (string.IsNullOrWhiteSpace(customDisplayName)) return;
        _renamedOverrides[appId] = customDisplayName;
        if (_indexedApps.TryGetValue(appId, out var app))
        {
            app.CustomDisplayName = customDisplayName;
        }
    }

    public async Task<bool> CleanUninstallAsync(string appId, bool moveLeftoversToRecycleBin = true, CancellationToken ct = default)
    {
        if (!_indexedApps.TryGetValue(appId, out var app))
        {
            return false;
        }

        if (app.AppType == AppType.MsixPackage)
        {
            try
            {
                var packageManager = new PackageManager();
                var deploymentOperation = packageManager.RemovePackageAsync(app.ExecutablePath);
                await deploymentOperation.AsTask(ct).ConfigureAwait(false);
                _indexedApps.TryRemove(appId, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Desktop App Uninstall: clean leftovers to Recycle Bin
        if (moveLeftoversToRecycleBin)
        {
            var appName = app.Name;
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            StageFolderToRecycleBin(Path.Combine(localAppData, appName));
            StageFolderToRecycleBin(Path.Combine(appData, appName));
        }

        _indexedApps.TryRemove(appId, out _);
        return true;
    }

    private static void StageFolderToRecycleBin(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
        }
        catch { /* Continue on partial cleanup */ }
    }
}
