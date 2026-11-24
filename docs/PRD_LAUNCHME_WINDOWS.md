# Product Requirements Document: LaunchMe-Win (Parity with LaunchMe macOS v3.01.0)

Source specification derived from LaunchMe official documentation (`https://launchmeapp.com/`, `/help/`, `/changelog/`, `/music/`, `/spaces-themes/`).

## 1. Core Architectural Principle
One unified local **Spaces State Engine** synchronizes four visual shells (Full-Screen Launcher, Dynamic Notch, Multi-Display Dock, Full-Screen Music Player). Custom app icons, renamed labels, Workflows, and widgets defined once propagate automatically across all shells. **Pause-on-Hide GPU Rule:** when the Full-Screen Launcher is hidden, all video decoding, animations, and wallpaper shaders MUST pause immediately (`0%` idle GPU).

## 2. The Four UI Surfaces

### 2.1 Full-Screen Launcher
- **Classic View & Free Layout:** Classic View auto-populates all indexed apps; Edit Mode allows placing apps, folders, and widgets in arbitrary grid cells (including empty gaps) and dragging across pages via a highlighted page switcher.
- **Smart Folders:** Drag-on-top creation, Shift-multi-select batch creation, A–Z sort, configurable folder grids (`3x5`–`3x8`, `4x5`–`4x8`, `5x5`–`5x8`), auto-grouping by category, and layout save/restore.
- **Embedded Windows Folders:** Attach any local/network folder inside a Launcher folder with custom HEX color, emoji, gallery cover, or uploaded image icons.
- **Categories & App Management:** Categories page (Auto-grouped, Recently Used, Hidden). Hide apps (still searchable), Hide titles, non-destructive Rename apps, Extra App Sources (scan custom folders/external drives), and **Clean Uninstall** (runs app uninstaller + moves leftover `%AppData%`/`%LocalAppData%` traces to Recycle Bin).
- **Multi-Monitor & Windowing:** Opens on the monitor under the cursor; supports *Show over fullscreen apps*, *Always On* mode on dedicated displays, touchscreen support, and empty-space right-click context menu.

### 2.2 Dynamic Notch
- **Compact Notch HUD:** Top-center bar with user-adjustable width and height. Displays live Weather, Now Playing art + time, Battery, Bluetooth device toasts, Focus mode, and active Timer countdown. Must auto-hide in fullscreen games and exclude itself from screen capture (`WDA_EXCLUDEFROMCAPTURE`).
- **Expanded Drawer:** Opens on hover or click (configurable). Hosts customizable Widgets (Compact / Large size via context menu) plus an Apps/Folders/Workflows slide-down widget.
- **Notch Clipboard:** Visual preview cards with drag-reorderable categories: *Recent, Images, Colors, Text, Files, Code, Screenshots, Favorites*. Supports dragging images directly into external apps.
- **Share & Pocket Drop Zones:** Dragging any file onto the Notch auto-expands two zones: Windows Nearby Share (`DataTransferManager`) and **Pocket** (temporary multi-file shelf with drag-out support).
- **Timer & Pomodoro:** Built-in countdown and Pomodoro focus widget with selectable alert sounds and completion animation.
- **Per-Space Styling:** *Black, Clear Liquid Glass, Black + Liquid Glass,* or *Custom Image* synchronized automatically on Space switch.

### 2.3 Multi-Display Dock
- **Taskbar Replacement & Floating Mode:** Auto-hides native Windows Taskbar when enabled (cursor-to-bottom reveals native Taskbar), OR runs in *Floating Mode* positioned anywhere.
- **Per-Monitor & Dock Spaces:** Independent Dock layout per external monitor, ready-made presets, and two-finger swipe between Dock Spaces.
- **Maximized Window Controls:** Option in Dock Settings to either auto-hide Dock for maximized windows or reserve desktop work area (`SHAppBarMessage`) so maximized windows snap cleanly above the Dock.
- **Dock-Exclusive Widgets:** System Stats (CPU/RAM/Net), Ambient Background Sound with animation, Wallpaper Engine (abstract presets + Ring-Styled image converter with ring size and brightness sliders), Compact Music player (auto-compacts when idle), and **Tool Bar** (Touch Bar–inspired quick action strip).

### 2.4 Full-Screen Music Player
- **Media Control:** Integrates with Windows GSMTC (Spotify, Apple Music, YouTube Music, browsers).
- **Visual Modes:** *Vinyl* (rotating record + *Waves* style), *Full Screen Art* (animated cover art), *Particles*, *Glowing Beats*, plus vertical layout. Strips search bar and menu chrome in full-screen mode; supports *Music Art Wallpaper*.

## 3. Widget Catalog (11 Families)
1. **Pixel Art Animations:** 18 looping animated characters.
2. **Clock:** 12 styles (including Simple Squared).
3. **Weather:** 9 styles (Minimal, Compact, Forecast, Panel, Almanac, Horizon, Bold, Story + extended) × 4 sizes.
4. **Music:** 6 styles (Minimal, Full Cover, Vinyl, 2 dynamic artwork-color styles, Compact Dock idle mode) × 3 sizes.
5. **Photo:** 4 sizes, JPG/PNG with transparent background + custom fill.
6. **Shape Photo:** 4 sizes with customizable shape masks.
7. **GIF:** 4 sizes, animated GIFs with transparent background support.
8. **Affirmation / Text:** 4 sizes, custom font, text size, color, background.
9. **Calendar:** 3 sizes, custom typography and background.
10. **Notch Timer & Pomodoro:** 2 sizes (Compact / Large) with sound alerts.
11. **Dock Utilities:** System Stats, Ambient Sound, Wallpaper Engine, Tool Bar.

## 4. Unified Search, Workflows, Spaces & Visual Engine
- **Unified Search:** Single center-alignable input bar handling Apps, opt-in Files (Open, Copy File, Copy Path, Delete, Preview, Show in Explorer), Clipboard history, Inline Calculator, and Emoji & Symbols (`TAB` switch).
- **Workflows:** Multi-target launch preset (apps + files + folders) rendered as a single custom icon usable in Grid, Folders, or Notch.
- **Spaces & `.launchmetheme`:** Full environment snapshot switched via hotkey or day/time schedule; exportable as a single `.launchmetheme` ZIP bundle with double-click Windows shell association.
- **Visual Engine:** Liquid Glass (Clear or custom-HEX Tinted with system intensity sync) vs. Flat mode; System Dark/Light/Tinted icons, Clear Colored icon pack, custom 1:1 PNG/JPG icons with auto rounded-square mask; Static (JPG/PNG/HEIC), Dynamic (Light/Dark, Sun time-of-day, live city Weather, Music Art), and Live Video (MP4/MOV/AVI/MKV) wallpapers with blur.

## 5. Invocation, Privacy & Diagnostics
- **Triggers:** Custom global hotkey (default `Win+4` or `Alt+Space`, disableable), Hot Corners (with dwell threshold), 3-/4-/5-finger Precision Touchpad pinch to open and spread to close.
- **Performance:** Fast startup and smooth scrolling with 300–500+ indexed apps via pre-rendered 128px icon cache and virtualized grids.
- **Privacy & Diagnostics:** 100% local data storage; opt-in folder permissions for file search; built-in Diagnostics panel in Settings verifying hooks, DPI scaling, and cache health; localization support for 18 languages.
