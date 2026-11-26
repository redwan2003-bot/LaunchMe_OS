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
                if (package.IsFramework || package.IsResourcePackage || package.IsOptional ||
                    (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) && package.IsStub)) continue;

                var id = package.Id;
                if (string.IsNullOrWhiteSpace(id?.FamilyName)) continue;

                var displayName = package.DisplayName;
                if (string.IsNullOrWhiteSpace(displayName)) displayName = id.Name;
                if (string.IsNullOrWhiteSpace(displayName) ||
                    Guid.TryParse(displayName.Trim('{', '}'), out _) ||
                    IsInternalSystemHelper(displayName, id.Name)) continue;

                var appId = $"uwp:{id.FamilyName.ToLowerInvariant()}";
                string installPath = package.InstalledLocation?.Path ?? string.Empty;
                string? logoCandidate = null;
                try { logoCandidate = package.Logo?.LocalPath; } catch { }

                string? iconTarget = !string.IsNullOrWhiteSpace(logoCandidate) && File.Exists(logoCandidate)
                    ? logoCandidate
                    : installPath;
                var iconPath = _iconCache.GetOrExtractIcon(appId, iconTarget);
                var base64 = _iconCache.ConvertIconToBase64(iconPath);

                var item = new AppItem
                {
                    Id = appId,
                    Name = displayName,
                    ExecutablePath = installPath,
                    IconCachePath = iconPath,
                    IconBase64 = base64,
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

    private static bool IsInternalSystemHelper(string displayName, string packageName)
    {
        string combined = $"{displayName} {packageName}";
        string[] excludedTokens =
        [
            "Add Folder Suggestions", "App Resolver", "AsyncTextService",
            "Captive Portal", "CapturePicker", "Credential Dialog",
            "Desktop App Web Viewer", "BioEnrollment", "CallingShellApp",
            "ContentDeliveryManager", "LockApp", "NcsiUwpApp",
            "ParentalControls", "PeopleExperienceHost", "PinningConfirmation",
            "PrintDialog", "ShellExperienceHost", "StartMenuExperienceHost",
            "XboxGameCallableUI", "AssignedAccessLockApp", "EyeControl"
        ];
        return excludedTokens.Any(t => combined.Contains(t, StringComparison.OrdinalIgnoreCase));
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

    /// <summary>
    /// Returns indexed apps filtered by category name, or the virtual categories "All", "Hidden", and "Recently Used".
    /// </summary>
    public IReadOnlyList<AppItem> GetAppsByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return [];
        }

        var trimmed = category.Trim();
        var apps = _indexedApps.Values;

        IEnumerable<AppItem> filtered;
        if (string.Equals(trimmed, "All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = apps.Where(static a => !a.IsHidden);
        }
        else if (string.Equals(trimmed, "Hidden", StringComparison.OrdinalIgnoreCase))
        {
            filtered = apps.Where(static a => a.IsHidden);
        }
        else if (string.Equals(trimmed, "Recently Used", StringComparison.OrdinalIgnoreCase))
        {
            filtered = apps.Where(static a => a.IsRecentlyUsed);
        }
        else
        {
            filtered = apps.Where(a =>
                !a.IsHidden &&
                a.Categories.Any(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase)));
        }

        return filtered
            .OrderBy(static a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Returns the distinct, alphabetically sorted categories across all visible indexed apps.
    /// </summary>
    public IReadOnlyList<string> GetCategories()
    {
        return _indexedApps.Values
            .Where(static a => !a.IsHidden)
            .SelectMany(static a => a.Categories)
            .Where(static c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static c => c, StringComparer.OrdinalIgnoreCase)
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

    /// <summary>
    /// Returns existing safe leftover trace folders under %LocalAppData% and %AppData% for the specified desktop app.
    /// </summary>
    public IReadOnlyList<string> PreviewUninstallLeftovers(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId) || !_indexedApps.TryGetValue(appId, out var app))
        {
            return [];
        }

        if (app.AppType == AppType.MsixPackage || string.IsNullOrWhiteSpace(app.Name))
        {
            return [];
        }

        var results = new List<string>();
        foreach (var baseFolder in GetLeftoverBaseFolders())
        {
            var safePath = ResolveSafeLeftoverPath(baseFolder, app.Name);
            if (safePath is not null)
            {
                results.Add(safePath);
            }
        }

        return results;
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

        // Desktop App Uninstall: clean leftovers safely to Recycle Bin
        if (moveLeftoversToRecycleBin && !string.IsNullOrWhiteSpace(app.Name))
        {
            foreach (var baseFolder in GetLeftoverBaseFolders())
            {
                StageFolderToRecycleBin(baseFolder, app.Name);
            }
        }

        _indexedApps.TryRemove(appId, out _);
        return true;
    }

    private static IEnumerable<string> GetLeftoverBaseFolders()
    {
        var candidates = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        };

        return candidates
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? ResolveSafeLeftoverPath(string basePath, string appName)
    {
        if (string.IsNullOrWhiteSpace(appName) || string.IsNullOrWhiteSpace(basePath))
        {
            return null;
        }

        var trimmedName = appName.Trim();
        // Reject explicit traversal or separator sequences before extracting filename.
        if (trimmedName.Contains("..") ||
            trimmedName.Contains(Path.DirectorySeparatorChar) ||
            trimmedName.Contains(Path.AltDirectorySeparatorChar))
        {
            return null;
        }

        var sanitizedAppName = Path.GetFileName(trimmedName);
        if (string.IsNullOrWhiteSpace(sanitizedAppName) ||
            sanitizedAppName == "." ||
            sanitizedAppName == ".." ||
            sanitizedAppName.Contains(".."))
        {
            return null;
        }

        var fullBasePath = Path.GetFullPath(basePath);
        var targetPath = Path.GetFullPath(Path.Combine(fullBasePath, sanitizedAppName));
        var basePrefix = fullBasePath.EndsWith(Path.DirectorySeparatorChar)
            ? fullBasePath
            : fullBasePath + Path.DirectorySeparatorChar;

        // Ensure target is strictly a subfolder of basePath and never the basePath itself
        if (!targetPath.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase) ||
            targetPath.Equals(fullBasePath, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Directory.Exists(targetPath) ? targetPath : null;
    }

    private static void StageFolderToRecycleBin(string basePath, string appName)
    {
        var targetPath = ResolveSafeLeftoverPath(basePath, appName);
        if (targetPath is null)
        {
            return;
        }

        try
        {
            FileSystem.DeleteDirectory(targetPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }
        catch { /* Continue on partial cleanup */ }
    }
}
