using LaunchMe.Contracts.Clipboard;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Contracts.Media;
using LaunchMe.Contracts.Search;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Contracts.Triggers;
using LaunchMe.Contracts.Workflows;
using LaunchMe.Core.Apps;
using LaunchMe.Core.Files;
using LaunchMe.Core.Search;
using LaunchMe.Core.Spaces;
using LaunchMe.Core.Widgets.Weather;
using LaunchMe.Core.Workflows;
using LaunchMe.Platform.Win32.Clipboard;
using LaunchMe.Platform.Win32.Gestures;
using LaunchMe.Platform.Win32.Media;
using LaunchMe.Platform.Win32.Metrics;
using LaunchMe.Platform.Win32.Shell;
using LaunchMe.UI.Services;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Views;
using LaunchMe.UI.Widgets.Catalog;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace LaunchMe.UI;

/// <summary>
/// WinUI 3 Application composition root, lifecycle coordinator, and system trigger dispatcher.
/// </summary>
public partial class App : Application
{
    private DispatcherQueue? _dispatcherQueue;
    private SqliteSpaceStore? _spaceStore;
    private SpaceSchedulerService? _spaceScheduler;
    private WinRTClipboardMonitor? _clipboardMonitor;
    private WinRTMediaSessionService? _mediaSession;
    private Win32SystemMetricsService? _systemMetrics;
    private GestureAndTriggerService? _triggerService;
    private SystemTrayService? _trayService;
    private WidgetCatalogViewModel? _widgetCatalog;
    private LauncherViewModel? _launcherViewModel;
    private LauncherWindow? _launcherWindow;
    private IAppIndexer? _appIndexer;
    private bool _shuttingDown;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Build DI composition root
        _spaceStore = new SqliteSpaceStore();
        var iconCache = new IconCacheService();
        _appIndexer = new AppIndexer(iconCache);
        IClipboardWriter clipboardWriter = new Win32ClipboardWriter();
        _clipboardMonitor = new WinRTClipboardMonitor();
        IFileSearchService fileSearch = new FileSearchService(clipboardWriter);
        IAppLauncherService appLauncher = new AppLauncherService();
        ICalculatorService calculator = new InlineCalculatorService();
        IEmojiService emoji = new EmojiIndexService();
        _mediaSession = new WinRTMediaSessionService();
        _systemMetrics = new Win32SystemMetricsService();
        IWorkflowRunner workflowRunner = new WorkflowRunner(appLauncher);
        _spaceScheduler = new SpaceSchedulerService(_spaceStore);
        IThemeAssociationService themeAssociation = new ThemeFileAssociationService();
        var diagnosticsService = new PlatformDiagnosticsService();

        var statsAdapter = new Win32SystemStatsAdapter(_systemMetrics);
        var weatherAdapter = new CoreWeatherAdapter(new SimulatedWeatherProvider());
        _widgetCatalog = new WidgetCatalogViewModel(_mediaSession, weatherAdapter, statsAdapter);

        _launcherViewModel = new LauncherViewModel(
            _appIndexer,
            fileSearch,
            _clipboardMonitor,
            calculator,
            emoji,
            _spaceStore,
            appLauncher,
            clipboardWriter,
            _widgetCatalog,
            workflowRunner,
            _spaceScheduler,
            themeAssociation,
            diagnosticsService);

        _launcherViewModel.QuitAppRequested += OnQuitAppRequested;
        _spaceScheduler.ScheduledSpaceActivated += OnScheduledSpaceActivated;
        _spaceScheduler.Start();

        _launcherWindow = new LauncherWindow(_launcherViewModel);
        _launcherWindow.Closed += OnWindowClosed;

        // Global Hotkey (Alt+Space: modifiers = 1, vk = 0x20) + Hot Corners
        _triggerService = new GestureAndTriggerService();
        _triggerService.RegisterGlobalHotkey(1, 0x20);
        _triggerService.SetHotCorners(new HotCornerConfig
        {
            TopLeft = HotCornerAction.ToggleLauncher,
            DwellTimeMs = 150
        });
        _triggerService.Invoked += OnTriggerInvoked;

        // System Tray
        _trayService = new SystemTrayService();
        _trayService.ShowLauncherRequested += OnTrayShowLauncherRequested;
        _trayService.OpenSettingsRequested += OnTrayOpenSettingsRequested;
        _trayService.ExitRequested += OnTrayExitRequested;
        _trayService.Initialize("LaunchMe-Win");

        try
        {
            _clipboardMonitor.Start();
        }
        catch
        {
            // Clipboard hook optional in restricted desktop contexts
        }

        // Load active Space and run initial app scan asynchronously
        _ = InitializeSpaceAndAppsAsync();

        _launcherWindow.ShowLauncher();
    }

    private async Task InitializeSpaceAndAppsAsync()
    {
        if (_spaceStore != null)
        {
            try
            {
                var spaces = await _spaceStore.GetAllSpacesAsync().ConfigureAwait(false);
                var activeSpace = await _spaceStore.GetActiveSpaceAsync().ConfigureAwait(false);
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    if (_launcherViewModel is null) return;
                    _launcherViewModel.Spaces.Clear();
                    foreach (var space in spaces)
                    {
                        _launcherViewModel.Spaces.Add(space);
                    }
                    if (activeSpace != null)
                    {
                        _launcherViewModel.ApplySpace(activeSpace);
                    }
                });
            }
            catch
            {
                // Keep default visual state on startup store failure
            }
        }

        if (_appIndexer != null)
        {
            try
            {
                await _appIndexer.ScanAllAsync().ConfigureAwait(false);
                _dispatcherQueue?.TryEnqueue(() => _launcherViewModel?.LoadClassicView());
            }
            catch
            {
                // Ignore partial scan errors
            }
        }
    }

    private void OnScheduledSpaceActivated(object? sender, SpaceChangedEventArgs e)
    {
        if (e.ActiveSpace is null) return;
        _dispatcherQueue?.TryEnqueue(() => _launcherViewModel?.ApplySpace(e.ActiveSpace));
    }

    private void OnTriggerInvoked(object? sender, TriggerEventArgs e)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            if (_launcherWindow == null) return;
            if (_launcherWindow.IsLauncherVisible)
            {
                _launcherWindow.Dismiss();
            }
            else
            {
                _launcherWindow.ShowLauncher();
            }
        });
    }

    private void OnTrayShowLauncherRequested(object? sender, EventArgs e)
    {
        _dispatcherQueue?.TryEnqueue(() => _launcherWindow?.ShowLauncher());
    }

    private void OnTrayOpenSettingsRequested(object? sender, EventArgs e)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            _launcherWindow?.ShowLauncher();
            if (_launcherViewModel != null)
            {
                _launcherViewModel.IsSettingsOpen = true;
            }
        });
    }

    private void OnTrayExitRequested(object? sender, EventArgs e)
    {
        _dispatcherQueue?.TryEnqueue(Shutdown);
    }

    private void OnQuitAppRequested(object? sender, EventArgs e)
    {
        Shutdown();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        Shutdown();
    }

    private void Shutdown()
    {
        if (_shuttingDown) return;
        _shuttingDown = true;

        if (_spaceScheduler != null)
        {
            _spaceScheduler.ScheduledSpaceActivated -= OnScheduledSpaceActivated;
            _spaceScheduler.Stop();
            _spaceScheduler.Dispose();
            _spaceScheduler = null;
        }

        if (_launcherViewModel != null)
        {
            _launcherViewModel.QuitAppRequested -= OnQuitAppRequested;
            _launcherViewModel.Dispose();
            _launcherViewModel = null;
        }

        if (_triggerService != null)
        {
            _triggerService.Invoked -= OnTriggerInvoked;
            _triggerService.UnregisterGlobalHotkey();
            _triggerService.Dispose();
            _triggerService = null;
        }

        if (_trayService != null)
        {
            _trayService.ShowLauncherRequested -= OnTrayShowLauncherRequested;
            _trayService.OpenSettingsRequested -= OnTrayOpenSettingsRequested;
            _trayService.ExitRequested -= OnTrayExitRequested;
            _trayService.Dispose();
            _trayService = null;
        }

        if (_clipboardMonitor != null)
        {
            _clipboardMonitor.Stop();
            _clipboardMonitor.Dispose();
            _clipboardMonitor = null;
        }

        _widgetCatalog?.Dispose();
        _widgetCatalog = null;

        _systemMetrics?.Dispose();
        _systemMetrics = null;

        _mediaSession?.Dispose();
        _mediaSession = null;

        _spaceStore?.Dispose();
        _spaceStore = null;

        if (_launcherWindow != null)
        {
            _launcherWindow.Closed -= OnWindowClosed;
            _launcherWindow.Close();
            _launcherWindow = null;
        }

        Exit();
    }
}
