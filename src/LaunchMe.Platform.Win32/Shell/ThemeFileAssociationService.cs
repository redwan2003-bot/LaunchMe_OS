using LaunchMe.Contracts.Spaces;
using LaunchMe.Platform.Win32.Native;
using Microsoft.Win32;

namespace LaunchMe.Platform.Win32.Shell;

/// <summary>
/// Registers and unregisters per-user (HKCU) Windows shell file associations for portable .launchmetheme packages.
/// </summary>
public sealed class ThemeFileAssociationService : IThemeAssociationService
{
    public const string DefaultClassesRootSubKey = @"Software\Classes";
    public const string FileExtension = ".launchmetheme";
    public const string ProgId = "LaunchMeWin.Theme";
    public const string ContentType = "application/x-launchme-theme";
    public const string FriendlyTypeName = "LaunchMe Theme Package";

    private readonly string _classesRootSubKey;
    private readonly bool _notifyShell;

    public ThemeFileAssociationService(string? classesRootSubKey = null)
    {
        if (classesRootSubKey is null)
        {
            _classesRootSubKey = DefaultClassesRootSubKey;
        }
        else if (string.IsNullOrWhiteSpace(classesRootSubKey))
        {
            throw new ArgumentException("Classes root subkey cannot be empty or whitespace.", nameof(classesRootSubKey));
        }
        else
        {
            _classesRootSubKey = classesRootSubKey.Trim('\\');
        }

        _notifyShell = string.Equals(_classesRootSubKey, DefaultClassesRootSubKey, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsRegistered
    {
        get
        {
            using var rootKey = Registry.CurrentUser.OpenSubKey(_classesRootSubKey, writable: false);
            if (rootKey is null)
            {
                return false;
            }

            using var extensionKey = rootKey.OpenSubKey(FileExtension, writable: false);
            if (extensionKey?.GetValue(null) is not string mappedProgId ||
                !string.Equals(mappedProgId, ProgId, StringComparison.Ordinal))
            {
                return false;
            }

            using var commandKey = rootKey.OpenSubKey($@"{ProgId}\shell\open\command", writable: false);
            return commandKey?.GetValue(null) is string command && !string.IsNullOrWhiteSpace(command);
        }
    }

    public void RegisterFileAssociation(string executablePath)
    {
        ValidateExecutablePath(executablePath);

        using var extensionKey = Registry.CurrentUser.CreateSubKey($@"{_classesRootSubKey}\{FileExtension}", writable: true)
            ?? throw new InvalidOperationException($"Failed to create registry subkey for {FileExtension}.");
        extensionKey.SetValue(null, ProgId, RegistryValueKind.String);
        extensionKey.SetValue("Content Type", ContentType, RegistryValueKind.String);

        using var progIdKey = Registry.CurrentUser.CreateSubKey($@"{_classesRootSubKey}\{ProgId}", writable: true)
            ?? throw new InvalidOperationException($"Failed to create registry subkey for {ProgId}.");
        progIdKey.SetValue(null, FriendlyTypeName, RegistryValueKind.String);

        using var defaultIconKey = progIdKey.CreateSubKey("DefaultIcon", writable: true)
            ?? throw new InvalidOperationException("Failed to create DefaultIcon registry subkey.");
        defaultIconKey.SetValue(null, $"{executablePath},0", RegistryValueKind.String);

        using var commandKey = progIdKey.CreateSubKey(@"shell\open\command", writable: true)
            ?? throw new InvalidOperationException(@"Failed to create shell\open\command registry subkey.");
        commandKey.SetValue(null, $"\"{executablePath}\" --import-theme \"%1\"", RegistryValueKind.String);

        NotifyShellIfDefaultRoot();
    }

    public void UnregisterFileAssociation()
    {
        using var rootKey = Registry.CurrentUser.OpenSubKey(_classesRootSubKey, writable: true);
        if (rootKey is null)
        {
            return;
        }

        rootKey.DeleteSubKeyTree(FileExtension, throwOnMissingSubKey: false);
        rootKey.DeleteSubKeyTree(ProgId, throwOnMissingSubKey: false);

        NotifyShellIfDefaultRoot();
    }

    private void NotifyShellIfDefaultRoot()
    {
        if (_notifyShell)
        {
            Win32Interop.SHChangeNotify(
                Win32Interop.SHCNE_ASSOCCHANGED,
                Win32Interop.SHCNF_IDLIST,
                nint.Zero,
                nint.Zero);
        }
    }

    private static void ValidateExecutablePath(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        if (executablePath.Contains('"') ||
            executablePath.Contains('\r') ||
            executablePath.Contains('\n') ||
            executablePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            throw new ArgumentException("Executable path contains invalid or unsafe characters.", nameof(executablePath));
        }

        if (!Path.IsPathFullyQualified(executablePath))
        {
            throw new ArgumentException("Executable path must be a fully qualified path.", nameof(executablePath));
        }
    }
}
