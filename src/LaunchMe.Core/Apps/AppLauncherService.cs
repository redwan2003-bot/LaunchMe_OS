using System.Diagnostics;
using LaunchMe.Contracts.Indexing;
using Windows.Management.Deployment;

namespace LaunchMe.Core.Apps;

/// <summary>
/// Launches indexed Win32 shortcuts, portable executables, URLs, and packaged MSIX applications.
/// </summary>
public sealed class AppLauncherService : IAppLauncherService
{
    private readonly Func<ProcessStartInfo, bool> _processStarter;

    public AppLauncherService(Func<ProcessStartInfo, bool>? processStarter = null)
    {
        _processStarter = processStarter ?? (static psi => Process.Start(psi) != null);
    }

    public async Task<bool> LaunchAppAsync(AppItem app, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ct.ThrowIfCancellationRequested();

        bool launched = app.AppType == AppType.MsixPackage
            ? await LaunchMsixPackageAsync(app, ct).ConfigureAwait(false)
            : LaunchTarget(app.ExecutablePath, app.Arguments);

        if (launched)
        {
            app.IsRecentlyUsed = true;
            app.LastLaunchedUtc = DateTime.UtcNow;
        }

        return launched;
    }

    public bool LaunchTarget(string targetPath, string? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return false;
        }

        var trimmed = targetPath.Trim();
        string? launchTarget = null;

        if (File.Exists(trimmed) || Directory.Exists(trimmed))
        {
            launchTarget = trimmed;
        }
        else if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
                 (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            launchTarget = uri.AbsoluteUri;
        }

        if (launchTarget == null)
        {
            return false;
        }

        var psi = new ProcessStartInfo(launchTarget)
        {
            UseShellExecute = true
        };

        if (!string.IsNullOrWhiteSpace(arguments))
        {
            psi.Arguments = arguments;
        }

        try
        {
            return _processStarter(psi);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> LaunchMsixPackageAsync(AppItem app, CancellationToken ct)
    {
        var familyName = app.Id.StartsWith("uwp:", StringComparison.OrdinalIgnoreCase)
            ? app.Id[4..]
            : app.Id;

        try
        {
            var packageManager = new PackageManager();
            var packages = packageManager.FindPackagesForUser(string.Empty);

            foreach (var package in packages)
            {
                ct.ThrowIfCancellationRequested();

                bool familyMatch = !string.IsNullOrWhiteSpace(familyName) &&
                    string.Equals(package.Id?.FamilyName, familyName, StringComparison.OrdinalIgnoreCase);

                bool locationMatch = false;
                if (!familyMatch && !string.IsNullOrWhiteSpace(app.ExecutablePath))
                {
                    var installedPath = TryGetInstalledPath(package);
                    locationMatch = !string.IsNullOrWhiteSpace(installedPath) &&
                        string.Equals(installedPath, app.ExecutablePath, StringComparison.OrdinalIgnoreCase);
                }

                if (!familyMatch && !locationMatch)
                {
                    continue;
                }

                var entries = await package.GetAppListEntriesAsync().AsTask(ct).ConfigureAwait(false);
                if (entries is { Count: > 0 })
                {
                    return await entries[0].LaunchAsync().AsTask(ct).ConfigureAwait(false);
                }

                return false;
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Headless, non-packaged, or WinRT runtime fallback
            return false;
        }
    }

    private static string? TryGetInstalledPath(Windows.ApplicationModel.Package package)
    {
        try
        {
            return package.InstalledLocation?.Path;
        }
        catch
        {
            return null;
        }
    }
}
