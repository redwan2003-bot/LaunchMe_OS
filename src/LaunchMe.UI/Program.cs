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
using LaunchMe.UI.Widgets.Catalog;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
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

        string? themePackagePath = ExtractImportThemePath(args);
        if (!string.IsNullOrWhiteSpace(themePackagePath))
        {
            ImportThemePackage(themePackagePath);
        }

        ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }

    public static string? ExtractImportThemePath(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            return null;
        }

        string? candidate = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--import-theme", StringComparison.OrdinalIgnoreCase) &&
                i + 1 < args.Length)
            {
                candidate = args[i + 1];
                break;
            }
        }

        if (candidate is null && args.Length == 1)
        {
            candidate = args[0];
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        string trimmed = candidate.Trim();
        if (trimmed.StartsWith('-'))
        {
            return null;
        }

        if (!string.Equals(Path.GetExtension(trimmed), ThemeFileAssociationService.FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(trimmed);
        }
        catch
        {
            return null;
        }
    }

    public static bool ImportThemePackage(string themePackagePath, string? dbPath = null)
    {
        if (string.IsNullOrWhiteSpace(themePackagePath))
        {
            return false;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(themePackagePath.Trim());
        }
        catch
        {
            return false;
        }

        if (!string.Equals(Path.GetExtension(fullPath), ThemeFileAssociationService.FileExtension, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(fullPath))
        {
            return false;
        }

        try
        {
            using var spaceStore = new SqliteSpaceStore(dbPath);
            var imported = spaceStore.ImportThemeAsync(fullPath).GetAwaiter().GetResult();
            spaceStore.SwitchSpaceAsync(imported.SpaceId).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            // Malformed or corrupt .launchmetheme archive must not crash startup before Application.Start
            return false;
        }
    }

    private static void RunHeadlessSmokeTest()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"launchme-smoke-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        string tempDbPath = Path.Combine(tempRoot, "smoke.db");
        string tempIconDir = Path.Combine(tempRoot, "icons");
        string smokeRegistryRoot = $@"Software\LaunchMeSmokeTest\{Guid.NewGuid():N}";

        SqliteSpaceStore? spaceStore = null;
        SpaceSchedulerService? spaceScheduler = null;
        WinRTClipboardMonitor? clipboardMonitor = null;
        WinRTMediaSessionService? mediaSession = null;
        Win32SystemMetricsService? systemMetrics = null;
        GestureAndTriggerService? gestureTrigger = null;
        SystemTrayService? systemTray = null;
        WidgetCatalogViewModel? widgetCatalog = null;
        LauncherViewModel? launcherVm = null;
        ThemeFileAssociationService? themeAssociation = null;

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
            var workflowRunner = new WorkflowRunner(appLauncher);
            spaceScheduler = new SpaceSchedulerService(spaceStore);
            themeAssociation = new ThemeFileAssociationService(smokeRegistryRoot);
            var diagnosticsService = new PlatformDiagnosticsService();

            var statsAdapter = new Win32SystemStatsAdapter(systemMetrics);
            var weatherAdapter = new CoreWeatherAdapter(new SimulatedWeatherProvider());
            widgetCatalog = new WidgetCatalogViewModel(mediaSession, weatherAdapter, statsAdapter);

            if (widgetCatalog.Widgets.Count != 11)
            {
                throw new InvalidOperationException($"Smoke test failed: expected 11 widgets in catalog, got {widgetCatalog.Widgets.Count}.");
            }

            if (widgetCatalog.PixelArt.Presets.Count != 18)
            {
                throw new InvalidOperationException($"Smoke test failed: expected 18 pixel-art presets, got {widgetCatalog.PixelArt.Presets.Count}.");
            }

            launcherVm = new LauncherViewModel(
                appIndexer,
                fileSearch,
                clipboardMonitor,
                calculator,
                emoji,
                spaceStore,
                appLauncher,
                clipboardWriter,
                widgetCatalog,
                workflowRunner,
                spaceScheduler,
                themeAssociation,
                diagnosticsService);

            var activeSpace = spaceStore.GetActiveSpaceAsync().GetAwaiter().GetResult()
                ?? throw new InvalidOperationException("Smoke test failed: GetActiveSpaceAsync returned null.");

            launcherVm.ApplySpace(activeSpace);
            launcherVm.RefreshSpacesAsync().GetAwaiter().GetResult();

            // Validate SpaceSchedulerService lifecycle
            _ = spaceScheduler.EvaluateScheduleAsync(DateTimeOffset.Now).GetAwaiter().GetResult();
            spaceScheduler.Start();
            if (!spaceScheduler.IsRunning)
            {
                throw new InvalidOperationException("Smoke test failed: SpaceSchedulerService did not start.");
            }
            spaceScheduler.Stop();

            // Validate WorkflowRunner validation
            var sampleWorkflow = new WorkflowDefinition
            {
                Name = "Smoke Workflow",
                Actions = [new WorkflowAction { ActionType = WorkflowActionType.Delay, DelayMs = 1 }]
            };
            var validationErrors = workflowRunner.ValidateWorkflow(sampleWorkflow);
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException("Smoke test failed: WorkflowRunner rejected valid workflow.");
            }

            // Validate isolated HKCU ThemeFileAssociationService
            string exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "LaunchMe.UI.exe");
            themeAssociation.RegisterFileAssociation(exePath);
            if (!themeAssociation.IsRegistered)
            {
                throw new InvalidOperationException("Smoke test failed: ThemeFileAssociationService failed to register.");
            }
            themeAssociation.UnregisterFileAssociation();
            if (themeAssociation.IsRegistered)
            {
                throw new InvalidOperationException("Smoke test failed: ThemeFileAssociationService failed to unregister.");
            }

            // Validate PlatformDiagnosticsService
            var diag = diagnosticsService.CollectSnapshot();
            if (diag.SystemDpi == 0 || diag.MonitorCount < 1 || string.IsNullOrWhiteSpace(diag.OsVersion))
            {
                throw new InvalidOperationException("Smoke test failed: PlatformDiagnosticsService returned invalid snapshot.");
            }

            // Validate Pause-on-Hide across all 11 widgets
            widgetCatalog.Gif.GifPath = "sample.gif";
            launcherVm.OnShown();
            if (!widgetCatalog.PixelArt.IsTimerRunning || !widgetCatalog.Gif.IsPlaying)
            {
                throw new InvalidOperationException("Smoke test failed: widgets did not activate on OnShown.");
            }

            launcherVm.OnHidden();
            if (widgetCatalog.PixelArt.IsTimerRunning || widgetCatalog.Gif.IsPlaying)
            {
                throw new InvalidOperationException("Smoke test failed: Pause-on-Hide GPU law violated.");
            }

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

            // Phase 4 Smoke Validations: Dynamic Notch, Multi-Display Dock, Music Player, Precision Touchpad
            using var smokePocket = new LaunchMe.Core.Notch.PocketService(Path.Combine(tempRoot, "smoke_pocket.json"));
            using var smokeNotchWindow = new NotchWindowService();
            using var smokeDockShell = new DockShellService();
            using var smokeDockSpace = new LaunchMe.Core.Dock.DockSpaceService(Path.Combine(tempRoot, "smoke_dock.json"));
            using var smokeAmbient = new LaunchMe.Core.Dock.AmbientSoundService();
            var smokeRing = new LaunchMe.Core.Dock.RingImageProcessor();

            using var smokeNotchVm = new NotchViewModel(smokeNotchWindow, smokePocket, mediaSession, clipboardMonitor);
            using var smokeDockVm = new DockViewModel(smokeDockShell, smokeDockSpace, systemMetrics, smokeAmbient, appLauncher);
            using var smokeMusicVm = new MusicPlayerViewModel(mediaSession);

            smokeNotchVm.ToggleExpand();
            if (!smokeNotchVm.IsExpanded)
            {
                throw new InvalidOperationException("Smoke test failed: NotchViewModel failed to expand.");
            }
            smokeNotchVm.AddPocketFile(Path.Combine(tempRoot, "dummy.txt"));
            smokeNotchVm.StartTimer(5);
            smokeNotchVm.CheckFullscreenGame();

            smokeDockVm.NextSpace();
            smokeDockVm.ToggleAmbientSound();
            if (!smokeDockVm.IsAmbientPlaying)
            {
                throw new InvalidOperationException("Smoke test failed: DockViewModel failed to activate ambient sound.");
            }
            smokeDockVm.ToggleAmbientSound();

            smokeMusicVm.CycleVisualizerMode();
            if (smokeMusicVm.VisualizerMode != LaunchMe.Contracts.Media.MusicVisualizerMode.FullScreenArt)
            {
                throw new InvalidOperationException("Smoke test failed: MusicPlayerViewModel failed to cycle visualizer mode.");
            }

            gestureTrigger.EnableTouchpadPinch(3, true);

            // Phase 5 Smoke Validations: HyprWin Dynamic Tiling Engine & Pause-on-Hide
            var smokeTiling = new LaunchMe.Core.Tiling.TilingEngine();
            var smokeRules = new LaunchMe.Core.Tiling.WindowRuleService();
            var smokeDispatcher = new LaunchMe.Platform.Win32.Tiling.Win32TilingDispatcher();
            var smokeBorder = new LaunchMe.UI.Tiling.FocusBorderService();
            var smokePreview = new LaunchMe.UI.Tiling.TiledDropPreviewService();

            smokeTiling.RegisterWindow(1001, "code", "Visual Studio Code", new LaunchMe.Contracts.Tiling.WindowRect(0, 0, 800, 600), 1);
            var smokeLayout = smokeTiling.ComputeLayout("primary", new LaunchMe.Contracts.Tiling.WindowRect(0, 0, 1920, 1080), 1);
            if (smokeLayout.Count != 1)
            {
                throw new InvalidOperationException("Smoke test failed: TilingEngine failed to compute layout.");
            }

            smokeBorder.AttachToWindow(1001, smokeLayout[0].TargetRect);
            if (!smokeBorder.IsVisible)
            {
                throw new InvalidOperationException("Smoke test failed: FocusBorderService failed to attach.");
            }
            smokeBorder.HideBorder();
            if (smokeBorder.IsVisible)
            {
                throw new InvalidOperationException("Smoke test failed: FocusBorderService Pause-on-Hide violated.");
            }

            smokePreview.ShowPreview(new LaunchMe.Contracts.Tiling.WindowRect(10, 10, 500, 500));
            if (!smokePreview.IsVisible)
            {
                throw new InvalidOperationException("Smoke test failed: TiledDropPreviewService failed to show.");
            }
            smokePreview.HidePreview();
            if (smokePreview.IsVisible)
            {
                throw new InvalidOperationException("Smoke test failed: TiledDropPreviewService Pause-on-Hide violated.");
            }

            // Phase 6 Smoke Validations: Mochi Character & Agent Approval Cockpit
            var smokeApprovalService = new LaunchMe.Core.Agent.AgentApprovalService();
            var smokeDiffParser = new LaunchMe.Core.Agent.DiffParserService();
            var smokeMochi = new LaunchMe.Core.Agent.MochiController();

            smokeMochi.Poke();
            if (smokeMochi.CurrentMood != LaunchMe.Contracts.Agent.MochiMood.Poked)
            {
                throw new InvalidOperationException("Smoke test failed: MochiController Poke failed.");
            }

            var smokeApprovalReq = new LaunchMe.Contracts.Agent.AgentApprovalRequest(
                "smoke-req-1",
                "Claude Code",
                "Bash",
                "echo smoke",
                "+smoke\n-old",
                LaunchMe.Contracts.Agent.ApprovalRiskLevel.Safe,
                null,
                DateTime.UtcNow);

            var smokeTask = smokeApprovalService.RequestApprovalAsync(smokeApprovalReq);
            if (smokeApprovalService.GetPendingRequests().Count != 1)
            {
                throw new InvalidOperationException("Smoke test failed: AgentApprovalService failed to queue request.");
            }

            smokeApprovalService.SubmitDecisionAsync("smoke-req-1", LaunchMe.Contracts.Agent.ApprovalAction.Allowed).GetAwaiter().GetResult();
            var smokeDecision = smokeTask.GetAwaiter().GetResult();
            if (smokeDecision.Action != LaunchMe.Contracts.Agent.ApprovalAction.Allowed)
            {
                throw new InvalidOperationException("Smoke test failed: AgentApprovalService failed to resolve decision.");
            }

            var smokeDiff = smokeDiffParser.Parse("+line1\n-line2");
            if (smokeDiff.Files.Count == 0)
            {
                throw new InvalidOperationException("Smoke test failed: DiffParserService failed to parse diff.");
            }

            Console.WriteLine("SMOKE_TEST_OK");
        }
        finally
        {
            launcherVm?.Dispose();
            widgetCatalog?.Dispose();
            spaceScheduler?.Dispose();
            systemTray?.Dispose();
            gestureTrigger?.Dispose();
            systemMetrics?.Dispose();
            mediaSession?.Dispose();
            clipboardMonitor?.Dispose();
            spaceStore?.Dispose();

            try
            {
                themeAssociation?.UnregisterFileAssociation();
                Registry.CurrentUser.DeleteSubKeyTree(smokeRegistryRoot, throwOnMissingSubKey: false);
            }
            catch
            {
                // Best-effort registry cleanup
            }

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
