# Architecture & Frozen Interface Contracts

## Solution Layout
```text
LaunchMeWin.sln
├── src/
│   ├── LaunchMe.Contracts/          # Phase 0 ONLY (Orchestrator). Frozen DTOs & interfaces.
│   ├── LaunchMe.Core/               # Owned by: core-engine-architect
│   ├── LaunchMe.Platform.Win32/     # Owned by: win32-hooks-engineer
│   └── LaunchMe.UI/                 # Owned by: ui-shell-builder (WinUI 3 Desktop App)
└── tests/
    ├── LaunchMe.Core.Tests/         # Owned by: core-engine-architect
    ├── LaunchMe.Platform.Tests/     # Owned by: win32-hooks-engineer
    └── LaunchMe.UI.Tests/           # Owned by: ui-shell-builder
```

## Frozen Interfaces (Create in `src/LaunchMe.Contracts/` in Phase 0)
1. `ISpaceStore` — `GetAllSpacesAsync()`, `GetActiveSpaceAsync()`, `SwitchSpaceAsync(string spaceId)`, `ExportThemeAsync(string spaceId, string outputZipPath)`, `ImportThemeAsync(string themeZipPath)`, `event EventHandler<SpaceChangedEventArgs> SpaceChanged`.
2. `IAppIndexer` — `ScanAllAsync(CancellationToken ct)`, `AddExtraSource(string folderPath)`, `SearchApps(string query)`, `CleanUninstallAsync(string appId, bool moveLeftoversToRecycleBin)`.
3. `IFileSearchService` — `GrantFolderScope(string folderPath)`, `SearchFilesAsync(string query, CancellationToken ct)`, `CopyFileToClipboard(string path)`, `RevealInExplorer(string path)`.
4. `IWorkflowRunner` — `ExecuteWorkflowAsync(WorkflowDefinition workflow, CancellationToken ct)`.
5. `IClipboardMonitor` — `Start()`, `Stop()`, `GetItems(ClipboardCategory category)`, `PinFavorite(string itemId)`, `ReorderCategories(IReadOnlyList<ClipboardCategory> order)`, `event EventHandler<ClipboardEntry> ItemCaptured`.
6. `IMediaSessionService` — `GetCurrentTrackAsync()`, `PlayPauseAsync()`, `NextAsync()`, `PreviousAsync()`, `SeekAsync(TimeSpan pos)`, `event EventHandler<MediaTrackInfo> TrackChanged`.
7. `IGestureAndTriggerService` — `RegisterGlobalHotkey(int modifiers, int virtualKey)`, `SetHotCorners(HotCornerConfig config)`, `EnableTouchpadPinch(int fingerCount, bool enabled)`, `event EventHandler<TriggerEventArgs> Invoked`.
8. `IDockShellService` — `RegisterAppBar(nint hwnd, nint hMonitor, int heightPx, bool reserveWorkArea)`, `UnregisterAppBar(nint hwnd)`, `SetNativeTaskbarAutoHide(bool autoHide)`.
9. `INotchWindowService` — `ApplyScreenCaptureExclusion(nint hwnd, bool exclude)`, `IsFullscreenGameActive()`.

## Canonical `.launchmetheme` Bundle Schema (`manifest.json` inside ZIP)
```json
{
  "schemaVersion": "3.01",
  "spaceId": "work-focus",
  "name": "Deep Work",
  "schedule": { "days": ["Mon","Tue","Wed","Thu","Fri"], "start": "09:00", "end": "18:00" },
  "hotkey": "Ctrl+Alt+1",
  "theme": { "style": "LiquidGlass", "mode": "Tinted", "tintHex": "#1E40AF", "intensity": 0.65, "titleColor": "Auto" },
  "wallpaper": { "type": "LiveVideo", "asset": "assets/bg.mp4", "blur": 0.25, "pauseOnHide": true },
  "launcher": {
    "classicView": true, "freeLayout": true, "hideTitles": false, "iconScale": 1.0,
    "folderGrid": "4x6", "pages": []
  },
  "notch": { "enabled": true, "bg": "BlackGlass", "openMode": "Hover", "hideInGames": true, "excludeCapture": true, "widgets": ["Music:Compact", "Timer:Compact"] },
  "dock": { "perDisplay": true, "floating": false, "maxWindowMode": "ReserveWorkArea", "widgets": ["SystemStats", "MusicCompact", "ToolBar"] },
  "workflows": []
}
```
