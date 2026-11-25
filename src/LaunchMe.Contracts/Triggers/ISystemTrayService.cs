namespace LaunchMe.Contracts.Triggers;

/// <summary>
/// Manages the Windows notification area (system tray) icon and context menu for LaunchMe-Win.
/// </summary>
public interface ISystemTrayService : IDisposable
{
    bool IsInitialized { get; }
    void Initialize(string tooltip = "LaunchMe-Win");

    event EventHandler? ShowLauncherRequested;
    event EventHandler? OpenSettingsRequested;
    event EventHandler? ExitRequested;
}
