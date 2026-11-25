using LaunchMe.Core.Apps;
using LaunchMe.Core.Files;
using LaunchMe.Core.Search;
using LaunchMe.Core.Spaces;
using LaunchMe.Core.Widgets.Weather;
using LaunchMe.Platform.Win32.Clipboard;
using LaunchMe.Platform.Win32.Gestures;
using LaunchMe.Platform.Win32.Media;
using LaunchMe.Platform.Win32.Metrics;
using LaunchMe.Platform.Win32.Shell;
using LaunchMe.UI.Services;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Catalog;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace LaunchMe.UI;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--smoke-test", StringComparison.OrdinalIgnoreCase)))
        {
            RunHeadlessSmokeTest();
            return;
        }

        ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }

    private static void RunHeadlessSmokeTest()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"launchme-smoke-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        string tempDbPath = Path.Combine(tempRoot, "smoke.db");
        string tempIconDir = Path.Combine(tempRoot, "icons");

        SqliteSpaceStore? spaceStore = null;
        WinRTClipboardMonitor? clipboardMonitor = null;
        WinRTMediaSessionService? mediaSession = null;
        Win32SystemMetricsService? systemMetrics = null;
        GestureAndTriggerService? gestureTrigger = null;
        SystemTrayService? systemTray = null;
        WidgetCatalogViewModel? widgetCatalog = null;
        LauncherViewModel? launcherVm = null;

        try
        {
            spaceStore = new SqliteSpaceStore(tempDbPath);
            var iconCache = new IconCacheService(tempIconDir);
            var appIndexer = new AppIndexer(iconCache);
            var clipboardWriter = new Win32ClipboardWriter();
            clipboardMonitor = new WinRTClipboardMonitor();
            var fileSearch = new FileSearchService(clipboardWriter);
            var appLauncher = new AppLauncherService();
            var calculator = new InlineCalculatorService();
            var emoji = new EmojiIndexService();
            mediaSession = new WinRTMediaSessionService();
            systemMetrics = new Win32SystemMetricsService();
            gestureTrigger = new GestureAndTriggerService();
            systemTray = new SystemTrayService();

            var statsAdapter = new Win32SystemStatsAdapter(systemMetrics);
            var weatherAdapter = new CoreWeatherAdapter(new SimulatedWeatherProvider());
            widgetCatalog = new WidgetCatalogViewModel(mediaSession, weatherAdapter, statsAdapter);

            launcherVm = new LauncherViewModel(
                appIndexer,
                fileSearch,
                clipboardMonitor,
                calculator,
                emoji,
                spaceStore,
                appLauncher,
                clipboardWriter,
                widgetCatalog);

            var activeSpace = spaceStore.GetActiveSpaceAsync().GetAwaiter().GetResult()
                ?? throw new InvalidOperationException("Smoke test failed: GetActiveSpaceAsync returned null.");

            launcherVm.ApplySpace(activeSpace);
            launcherVm.OnShown();
            launcherVm.OnHidden();

            _ = statsAdapter.GetSnapshotAsync().GetAwaiter().GetResult();
            _ = weatherAdapter.GetWeatherDataAsync("Cupertino").GetAwaiter().GetResult();

            var calc = calculator.Evaluate("21*2");
            if (calc is not { IsValid: true, FormattedResult: "42" })
            {
                throw new InvalidOperationException("Smoke test failed: calculator verification failed.");
            }

            if (emoji.SearchEmojis("rocket").Count == 0)
            {
                throw new InvalidOperationException("Smoke test failed: emoji index returned no results.");
            }

            Console.WriteLine("SMOKE_TEST_OK");
        }
        finally
        {
            launcherVm?.Dispose();
            widgetCatalog?.Dispose();
            systemTray?.Dispose();
            gestureTrigger?.Dispose();
            systemMetrics?.Dispose();
            mediaSession?.Dispose();
            clipboardMonitor?.Dispose();
            spaceStore?.Dispose();

            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
            catch
            {
                // Best-effort temp cleanup
            }
        }
    }
}
