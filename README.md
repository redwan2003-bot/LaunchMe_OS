# LaunchMe-Win (LaunchMe OS)

<p align="center">
  <img src="https://raw.githubusercontent.com/redwan2003-bot/LaunchMe_OS/main/docs/assets/banner.png" alt="LaunchMe-Win Banner" width="100%" onerror="this.style.display='none'"/>
</p>

<p align="center">
  <strong>Native Windows 11 Workspace Manager & Full-Screen Shell</strong><br>
  <em>Achieving complete 1:1 feature parity with LaunchMe macOS v3.01.0</em>
</p>

<p align="center">
  <a href="#build--test-status"><img src="https://img.shields.io/badge/build-passing-brightgreen?style=for-the-badge&logo=dotnet" alt="Build Status"/></a>
  <a href="#test-coverage"><img src="https://img.shields.io/badge/tests-420%2F420%20passing-success?style=for-the-badge&logo=xunit" alt="Test Coverage"/></a>
  <a href="#tech-stack"><img src="https://img.shields.io/badge/target-.NET%209%20%7C%20WinUI%203-blue?style=for-the-badge&logo=windows11" alt="Target Framework"/></a>
  <a href="#security-architecture--vulnerability-defenses"><img src="https://img.shields.io/badge/security-hardened%20%26%20verified-success?style=for-the-badge&logo=securityscorecard" alt="Security Verified"/></a>
  <a href="#architecture-guardrails"><img src="https://img.shields.io/badge/telemetry-strictly%20zero-critical?style=for-the-badge&logo=shield" alt="Zero Telemetry"/></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-Boost%201.0-orange?style=for-the-badge" alt="License"/></a>
</p>

---

## Overview

**LaunchMe-Win** is an ultra-fast, local-first Windows 11 workspace manager crafted in **C# 13, .NET 9, WinUI 3, Win2D, and CsWin32**. It combines the fluid ergonomics, design fidelity, and keyboard-centric workflows of LaunchMe v3.01.0 (macOS) with the power of Hyprland dynamic tiling window management (**HyprWin**) across five synchronized native surfaces: the **Full-Screen Launcher**, the **Dynamic Notch HUD**, the **Multi-Display Dock**, the **Full-Screen Music Player**, and the **HyprWin Dynamic Tiling Engine**.

### The Five Core Surfaces & Engines
1. **Full-Screen Launcher Grid:** Self-contained WinUI 3 application summoned via `Alt+Space`, configurable Hot Corners (`WH_MOUSE_LL` 150ms dwell), or 3/4/5-finger Precision Touchpad pinch. Hosts Classic View & Free Layout grids, Unified Search, Categories, Folders, and live Acrylic Liquid Glass.
2. **Dynamic Notch (`NotchWindow.xaml`):** Top-center status HUD excluded from screen capture (`WDA_EXCLUDEFROMCAPTURE`) and automatically hidden during 3D fullscreen games (`SHQueryUserNotificationState`). Features live weather, now-playing music, battery, focus status, Pomodoro countdown, and an expandable drawer with **Pocket** temporary file shelf and Windows Nearby Share drop zones.
3. **Multi-Display Dock (`DockWindow.xaml`):** Desktop dock with Win32 `SHAppBarMessage` work-area reservation or floating mode, per-monitor Dock Spaces, live System Stats mini-gauge, Ambient Sound soundscape generator (`Rain`, `OceanWaves`, `Forest`, `WhiteNoise`, `CafeStream`), and Wallpaper Engine concentric ring styling.
4. **Full-Screen Music Player (`MusicPlayerWindow.xaml`):** Immersive media interface integrating Windows GSMTC with 4 visualizer modes: *Vinyl* (rotating record + grooves), *FullScreenArt* (blurred animated cover art), *Particles*, and *GlowingBeats*, strictly adhering to the Pause-on-Hide GPU Law.
5. **HyprWin Dynamic Tiling Engine:** Automatic Binary Space Partitioning (BSP) window manager featuring Hyprland's **Dwindle spiral layout**, **Slot Memory** (minimized/hidden windows seamlessly snap back to their exact split upon unminimizing), atomic Win32 `DeferWindowPos` multi-window batching, live Win2D focus glow borders, acrylic ghost drop previews, and regex window rules.

---

## System Architecture

```mermaid
flowchart TD
    subgraph UI_Layer ["Presentation & Composition Root (LaunchMe.UI)"]
        AppHost["Program.cs & App.xaml.cs (Self-Contained WinUI 3 + --smoke-test + DI Root)"]
        LW["LauncherWindow (OverlappedPresenter + DesktopAcrylicBackdrop + WS_EX_TOOLWINDOW)"]
        NotchUI["NotchWindow (Dynamic Notch HUD + WDA_EXCLUDEFROMCAPTURE + Pocket + Pomodoro)"]
        DockUI["DockWindow (SHAppBarMessage Multi-Display Dock + Dock Spaces + Ambient Sound)"]
        MusicUI["MusicPlayerWindow (Full-Screen GSMTC Player + Vinyl / Waves / Particles Visualizers)"]
        VM["LauncherViewModel & NotchViewModel & DockViewModel & MusicPlayerViewModel"]
        GlassUI["LiquidGlassOverlayControl (Acrylic + Tint + Highlight)"]
        WallUI["LiveWallpaperControl (Pause-on-Hide Video / Dynamic / Blur)"]
        WidgetUI["Complete 11-Family Widget Catalog (18 PixelArt loops, Weather, Stats, Gif, Photo)"]
    end

    subgraph Contracts ["Frozen Contract Core (LaunchMe.Contracts)"]
        ISpace["ISpaceStore & ISpaceSchedulerService & IThemeAssociationService"]
        INotch["INotchWindowService & IPocketService"]
        IDock["IDockShellService & IDockSpaceService & IAmbientSoundService & IRingImageProcessor"]
        IMedia["IMediaSessionService & MusicVisualizerMode"]
        ITrigger["IGestureAndTriggerService & TriggerType (Pinch/Spread)"]
        ISearch["IFileSearchService & ICalculatorService & IEmojiService"]
        IApp["IAppIndexer & IAppLauncherService"]
        IClip["IClipboardMonitor & IClipboardWriter"]
        ITiling["ITilingEngine & IWindowRuleService & IFocusBorderService & ITiledDropPreviewService"]
    end

    subgraph Core_Layer ["Core Engine (LaunchMe.Core)"]
        TilingCore["BSPTree, DwindleLayout, SlotMemory, WindowRuleService & TilingEngine"]
        Sqlite["SqliteSpaceStore, SpaceSchedulerService & SpaceLayoutSerializer"]
        Pocket["PocketService (Local JSON Shelf + File Validation)"]
        DockSpace["DockSpaceService (Per-Display Active Space Cycling)"]
        Ambient["AmbientSoundService (Soundscape State Machine + Volume Clamping)"]
        RingImg["RingImageProcessor (Concentric Ring Mask + Brightness/Saturation Matrix)"]
        AppIdx["AppIndexer (Categories/Leftovers) & AppLauncherService"]
        FileIdx["FileSearchService (Approved Scopes + Clipboard Copy)"]
        CalcEng["InlineCalculatorService & EmojiIndexService"]
        ThemeEng["ThemeEngineService & WallpaperService (NOAA Sun Calculator)"]
    end

    subgraph Platform_Layer ["Native Interop (LaunchMe.Platform.Win32)"]
        TilingWin["Win32WindowTracker (SetWinEventHook) & Win32TilingDispatcher (DeferWindowPos)"]
        Touchpad["Win32TouchpadService & TouchpadGestureParser (HID Digitizer 3/4/5-Finger Pinch/Spread)"]
        AppBar["DockShellService (SHAppBarMessage ABM_NEW / SETPOS + Multi-Monitor Enum)"]
        NotchWin["NotchWindowService (WDA_EXCLUDEFROMCAPTURE + DirectX Fullscreen Check)"]
        Tray["SystemTrayService (Shell_NotifyIconW + TrackPopupMenu)"]
        Assoc["ThemeFileAssociationService (HKCU .launchmetheme + SHChangeNotify)"]
        HotCorners["Win32HotCornerService (WH_MOUSE_LL 150ms Dwell)"]
        Hotkeys["Win32HotkeyService (RegisterHotKey)"]
        MediaCtl["WinRTMediaSessionService (Windows.Media.Control GSMTC Bridge)"]
        Metrics["Win32SystemMetricsService (GetSystemTimes + GlobalMemoryStatusEx + GetIfTable2)"]
    end

    AppHost --> LW
    AppHost --> NotchUI
    AppHost --> DockUI
    AppHost --> MusicUI
    UI_Layer --> Contracts
    Core_Layer -.->|Implements| Contracts
    Platform_Layer -.->|Implements| Contracts
    VM --> ISpace
    VM --> INotch
    VM --> IDock
    VM --> IMedia
    VM --> ITrigger
```

---

## Solution Layout

```
LaunchMe OS/
├── .agents/                        # Multi-agent roles & verification rules
├── docs/                           # PRD, Architecture, and Workstream specs
│   ├── ARCHITECTURE_AND_CONTRACTS.md
│   ├── PARALLEL_WORKSTREAMS.md
│   └── PRD_LAUNCHME_WINDOWS.md
├── src/
│   ├── LaunchMe.Contracts/         # Frozen interfaces & DTOs (Zero drift)
│   ├── LaunchMe.Core/              # BSP Tree, DwindleLayout, SlotMemory, WindowRules, SQLite, Themes, Wallpapers
│   ├── LaunchMe.Platform.Win32/    # WinEvent Hooks, DeferWindowPos, Raw Input, AppBar, Notch exclusion, GSMTC, Hotkeys
│   └── LaunchMe.UI/                # WinUI 3 EXE, Dynamic Notch, Dock, Music Player, Launcher, Focus Border, 11 Widgets
└── tests/
    ├── LaunchMe.Core.Tests/        # 205 Unit Tests (BSPTree, Dwindle, SlotMemory, WindowRules, Pocket, Spaces)
    ├── LaunchMe.Platform.Tests/    # 117 Unit Tests (Tiling Dispatcher, Window Tracker, Touchpad, AppBar, GSMTC)
    └── LaunchMe.UI.Tests/          # 98 Unit Tests (FocusBorder, DropPreview, NotchVM, DockVM, MusicPlayerVM, Widgets)
```

---

## Implementation Roadmap & Status

| Phase | Description | Status | Test Suites | Verification Gate |
| :---: | :--- | :---: | :---: | :---: |
| **Phase 0** | **Contract Freeze & Scaffolding**<br>Solution structure, `Directory.Build.props`, `<TreatWarningsAsErrors>`, frozen interfaces | **Completed** | Verified | Zero Warnings |
| **Phase 1** | **Core Grid, Search, Indexers, Clipboard & Triggers**<br>WinUI 3 OverlappedPresenter, SQLite Spaces, Start Menu/MSIX indexer, Scoped Search, Inline Calc, WinRT Clipboard, `WH_MOUSE_LL` Hot Corners (150ms dwell), Hotkeys | **Completed** | **72 / 72 Passing** | **PASS** (`qa-verifier-auditor`) |
| **Phase 2** | **Theming, Liquid Glass, Live Wallpapers & Core Widgets**<br>Liquid Glass acrylic & tint engine, Pause-on-Hide live video wallpapers, 7 widget families, GSMTC media bridge, native Win32 hardware metrics, DirectX fullscreen game detection | **Completed** | **248 / 248 Passing** | **PASS** (`qa-verifier-auditor` + Security Audit) |
| **Phase 2.5** | **Desktop Integration & Runnable App Bootstrapping**<br>Self-contained WinUI 3 `App.xaml` DI root, `SystemTrayService`, `AppLauncherService`, `Win32ClipboardWriter`, `SpaceLayoutSerializer`, `DesktopAcrylicBackdrop`, live Widget Catalog drawer, Folder & Settings/Diagnostics UI, deterministic `IDisposable` cleanup | **Completed** | **306 / 306 Passing** | **PASS** (`qa-verifier-auditor` + `--smoke-test`) |
| **Phase 3** | **Spaces Scheduler, Workflows UI, `.launchmetheme` Shell Association & Complete 11-Widget Catalog**<br>`SpaceSchedulerService` daemon, `WorkflowRunner` + quick-launch UI, HKCU `.launchmetheme` double-click registration + `--import-theme` CLI, Categories & Clean Uninstall leftover preview, `PlatformDiagnosticsService`, and Pixel Art (18 loops), Photo, Shape Photo (6 masks), and Pause-on-Hide GIF widgets | **Completed** | **361 / 361 Passing** | **PASS** (`qa-verifier-auditor` + `--smoke-test`) |
| **Phase 4** | **Dynamic Notch, Multi-Display Dock, Music Player & HID Gestures**<br>Dynamic Notch HUD (`WDA_EXCLUDEFROMCAPTURE`, Pocket shelf, Pomodoro timer, DirectX game hiding), Multi-Display Dock (`SHAppBarMessage` work-area reservation / floating mode, Dock Spaces, Ambient Sound player, Wallpaper Engine rings), Full-Screen Music Player (GSMTC, Vinyl/Waves/Particles/Beats visualizers, Pause-on-Hide GPU Law), and Precision Touchpad Raw Input HID 3/4/5-finger pinch/spread gestures | **Completed** | **393 / 393 Passing** | **PASS** (`qa-verifier-auditor` + `--smoke-test`) |

---

## Verified Feature Capabilities

### 1. Dynamic Notch HUD (`NotchWindow.xaml` & `NotchViewModel.cs`)
- **Compact Status Pill:** Top-center anchor displaying live Weather, Now Playing track and artist, Battery percentage, Focus mode indicator, and active Pomodoro countdown timer.
- **Privacy & Screen Capture Exclusion:** Window affinity configured with `WDA_EXCLUDEFROMCAPTURE` (`Win32Interop.SetWindowDisplayAffinity`) ensuring the notch remains hidden from screen captures and stream broadcasts.
- **DirectX / Direct3D Fullscreen Auto-Hide:** Queries `SHQueryUserNotificationState` (`QUNS_RUNNING_D_3D_FULL_SCREEN`) and foreground window bounds against monitor dimensions, suppressing the notch automatically when games are active.
- **Pocket Shelf & Sharing Drop Zone:** Drag-and-drop file shelf backed by `PocketService` (`pocket.json`), with temporary file caching, deletion, and Windows Nearby Share trigger.
- **Pomodoro Focus Countdown:** Integrated 25-minute Pomodoro timer with start, pause, and reset controls running on a background `PeriodicTimer`.

### 2. Multi-Display Dock (`DockWindow.xaml` & `DockViewModel.cs`)
- **Taskbar Replacement & Floating Mode:** Integrates with `SHAppBarMessage` (`ABM_NEW`, `ABM_QUERYPOS`, `ABM_SETPOS`, `ABM_REMOVE`) to reserve desktop work area so maximized windows snap above the dock, with instant floating mode toggle.
- **Multi-Monitor Dock Spaces:** Supports independent dock layouts per display (`DockSpaceService`), cycling sequentially and circularly via `NextDockSpace` and `PreviousDockSpace`.
- **Dock-Exclusive Widgets:**
  - **System Performance Mini-Gauge:** Live CPU usage percentage, RAM load percentage, and network throughput (KB/s) sampled from `Win32SystemMetricsService`.
  - **Ambient Sound Player:** Procedural soundscapes (`Rain`, `OceanWaves`, `Forest`, `WhiteNoise`, `CafeStream`) with volume slider and toggle.
  - **Wallpaper Engine Ring Converter:** Transforms source wallpapers into concentric ring pattern graphics with configurable ring count, thickness, spacing, brightness, and saturation matrices.

### 3. Full-Screen Music Player (`MusicPlayerWindow.xaml` & `MusicPlayerViewModel.cs`)
- **Windows GSMTC Integration:** Real-time synchronization with active media apps (Spotify, Apple Music, YouTube Music, edge/chrome browsers), with transport actions (Play/Pause, Skip Next, Skip Previous, Seek).
- **4 Visualizer Modes:**
  - *Vinyl*: 30 FPS rotating record disc with grooves, center label, and waveform response.
  - *FullScreenArt*: Full-screen album cover art background with dynamic acrylic tint.
  - *Particles*: Ambient floating particle field.
  - *GlowingBeats*: Pulsing halo reacting rhythmically to track timeline progression.
- **Pause-on-Hide GPU Law Compliance:** Render transforms and position timers pause automatically when hidden (`0%` GPU load). Pressing `Esc` or clicking the close button dismisses cleanly.

### 4. Precision Touchpad Gestures (`TouchpadGestureParser.cs` & `Win32TouchpadService.cs`)
- **Raw Input HID Digitizer:** Hidden message window listening for `HID_USAGE_PAGE_DIGITIZER` (`0x0D`) / `HID_USAGE_DIGITIZER_TOUCH_PAD` (`0x05`) using `RIDEV_INPUTSINK`.
- **Centroid Spread Parsing:** Tracks multi-finger contact clusters (configurable for 3, 4, or 5 fingers).
- **Trigger Actions:** Distance contraction >15% triggers `TouchpadPinch` (opens Launcher); distance expansion >15% triggers `TouchpadSpread` (dismisses Launcher), with 300ms debounce protection.

### 5. Runnable Self-Contained WinUI 3 Launcher Grid & 11 Widgets
- **Instant Desktop Boot:** Unpackaged, self-contained desktop executable (`Program.cs`, `App.xaml.cs`) launching headlessly without MSIX container overhead.
- **Complete 11-Widget Catalog:** Hosted in `WidgetCatalogView` (Clock, Weather, Music, System Stats, Pomodoro, Affirmations, Calendar, 18 Pixel Art loops, Photo, Shape Photo, and GIF).
- **Automated Spaces & `.launchmetheme` Association:** `SpaceSchedulerService` switches environments on schedule; double-clicking `.launchmetheme` bundles imports them via `ThemeFileAssociationService`.

---

## Security Architecture & Vulnerability Defenses

| Threat Vector | Mitigation Strategy | Enforcement & Verification |
| :--- | :--- | :--- |
| **Dependency CVEs** | Strict package curation and automated audits. | Zero known vulnerable packages (`dotnet list package --vulnerable`). |
| **ZipSlip Path Traversal** | Extraction path validation against base canonical prefix. | `SqliteSpaceStore.ImportThemeAsync` validates canonical prefix and rejects `..` sequences. |
| **CLI Theme Import Validation** | Extension, flag, path, and corrupt archive error guards. | `Program.ExtractImportThemePath` & `ImportThemePackage` enforce `.launchmetheme`, reject `-` flags, and handle corrupt ZIPs safely. |
| **Shell Registry Injection** | Quote/newline validation and HKCU-only scoping. | `ThemeFileAssociationService.ValidateExecutablePath` validates path boundaries before writing `HKCU\Software\Classes`. |
| **Accidental / Traversal Deletion** | Target validation and root folder protection in clean uninstall. | `AppIndexer.CleanUninstallAsync` & `PreviewUninstallLeftovers` verify subfolders strictly beneath `%LocalAppData%` / `%AppData%`. |
| **Workflow Command Injection** | URL scheme whitelisting for `OpenUrl` and `LaunchTarget` actions. | `WorkflowRunner` restricts URL launches strictly to `http://` or `https://` schemes. |
| **Explorer Argument Breakout** | Stripping double quotes from paths in `RevealInExplorer`. | `FileSearchService.RevealInExplorer` sanitizes quotes prior to invoking `explorer.exe /select,"path"`. |
| **Expression Evaluation DoS** | Input length limits and max recursion depth tracking. | `InlineCalculatorService` rejects expressions exceeding 256 characters and bounds recursive descent to depth 32. |
| **Memory Bloat & Exhaustion** | Clipboard payload truncation for massive text entries. | `WinRTClipboardMonitor` caps text payloads to 100,000 characters prior to storage. |

---

## Building, Running, and Testing

### Prerequisites
- Windows 11 (Build 22621 / 22H2 or higher)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or .NET 10 with RollForward)

### Build the Solution
All projects enforce `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`:
```powershell
dotnet build LaunchMeWin.sln -warnaserror
```

### Run Headless Integration Smoke Test (All 4 Shells)
```powershell
& "src\LaunchMe.UI\bin\x64\Debug\net9.0-windows10.0.22621.0\LaunchMe.UI.exe" --smoke-test
```

### Launch the Desktop Application
```powershell
dotnet run --project src\LaunchMe.UI\LaunchMe.UI.csproj
```

### Run the Full Test Suite (420 Tests)
```powershell
dotnet test LaunchMeWin.sln --no-build
```

---

## Daily Update Log

### 2026-11-25
- **Phase 5 HyprWin Dynamic Tiling Milestone Completed (Coucou & HyprMac Integration):**
  - **Dwindle Spiral Tiling Engine (`BSPTree.cs` / `BSPNode.cs` / `DwindleLayout.cs`):** Mathematical binary space partitioning tree recursively partitioning the screen area along the longest dimension of the active window with golden-ratio split clamping (`[0.15, 0.85]`).
  - **Slot Memory (`SlotMemory.cs`):** Persistent layout restoration remembering partner window, side, axis, and ratio so minimized, hidden, or space-switched windows slide seamlessly back into their original slot upon unminimizing.
  - **Window Rules Engine (`WindowRuleService.cs`):** Process name and window title regex matching for automatic floating exceptions (Calculator, Settings, 1Password, Save/Open dialogs) and workspace affinity routing.
  - **Win32 Lifecycle Tracker (`Win32WindowTracker.cs`):** Low-level `SetWinEventHook` monitoring `EVENT_OBJECT_CREATE`, `EVENT_OBJECT_DESTROY`, `EVENT_OBJECT_SHOW`, `EVENT_OBJECT_HIDE`, and `EVENT_SYSTEM_FOREGROUND`, filtering out cloaked background UWP apps (`DwmGetWindowAttribute(DWMWA_CLOAKED)`), tooltips, and taskbar popups.
  - **Atomic Multi-Window Dispatcher (`Win32TilingDispatcher.cs`):** Batches window moves via Win32 `BeginDeferWindowPos`, `DeferWindowPos`, and `EndDeferWindowPos` in a single V-Sync frame with zero visual tearing or stutter.
  - **UI Focus Adornments (`FocusBorderService.cs` / `TiledDropPreviewService.cs`):** Active window glow border and semi-transparent acrylic drag-and-tile ghost preview adhering strictly to the Pause-on-Hide GPU Law.
  - **Full Test Suite & Smoke Verification:** All projects compiled cleanly under `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (**0 Warnings, 0 Errors**), full test suite passing at **420 / 420 unit tests** (Core: 205, Platform: 117, UI: 98), zero stubs, zero CVEs, and live smoke test passing (`SMOKE_TEST_OK`).
- **Phase 4 Parity Milestone Completed (The Final Parity Phase):**
  - **Dynamic Notch (`NotchWindow.xaml` / `NotchViewModel.cs` / `NotchWindowService.cs`):** Top-center HUD pill, screen capture exclusion (`WDA_EXCLUDEFROMCAPTURE`), DirectX fullscreen game auto-hide (`SHQueryUserNotificationState`), Pomodoro countdown timer, and Pocket multi-file drop shelf (`PocketService.cs`).
  - **Multi-Display Dock (`DockWindow.xaml` / `DockViewModel.cs` / `DockShellService.cs` / `DockSpaceService.cs`):** Win32 `SHAppBarMessage` work-area reservation and floating mode toggle, multi-monitor Dock Spaces navigation, System Stats mini-gauge, Ambient Sound soundscape generator (`Rain`, `Waves`, `Forest`, `WhiteNoise`, `CafeStream`), and Wallpaper Engine ring styling (`RingImageProcessor.cs`).
  - **Full-Screen Music Player (`MusicPlayerWindow.xaml` / `MusicPlayerViewModel.cs`):** Immersive media player connected to Windows GSMTC with 4 visualizer modes (*Vinyl*, *FullScreenArt*, *Particles*, *GlowingBeats*), seek controls, and strict Pause-on-Hide GPU Law compliance.
  - **Precision Touchpad Gestures (`TouchpadGestureParser.cs` / `Win32TouchpadService.cs`):** Win32 Raw Input HID Digitizer sink tracking 3/4/5-finger pinches to open Launcher and spreads to dismiss Launcher with debouncing.
  - **Full QA Audit & Verification:** `qa-verifier-auditor` signed off **PASS**; `dotnet build LaunchMeWin.sln -warnaserror` (**0 Warnings, 0 Errors**), **393 / 393 unit tests passing**, 0 CVEs, zero stubs across 154 source files, and live smoke test verified (`SMOKE_TEST_OK`, `EXIT_CODE=0`).
- **Phase 3 Parity Completed:** Spaces Scheduler, Workflows UI, HKCU `.launchmetheme` Shell Association, Categories & Clean Uninstall leftover preview, Complete 11-Widget Catalog (**361 / 361 tests passing**).
- **Phase 2.5 Integration Sprint Completed:** Self-contained WinUI 3 entrypoint, `SystemTrayService`, `AppLauncherService`, `Win32ClipboardWriter`, and `DesktopAcrylicBackdrop` (**306 / 306 tests passing**).

### 2026-11-24
- **Phases 0, 1, 2 & Security Hardening:** Initial scaffold, contracts freeze, SQLite spaces, app/file search, hotkeys/hot corners, WinRT clipboard, Liquid Glass, Pause-on-Hide wallpapers, initial 7 widgets, GSMTC, Win32 metrics, and security hardening (**248 / 248 tests passing**).
- **GitHub Repository Sync:** Synchronized with [redwan2003-bot/LaunchMe_OS](https://github.com/redwan2003-bot/LaunchMe_OS.git).

---

## License

This project is licensed under the [Boost Software License 1.0](LICENSE).
