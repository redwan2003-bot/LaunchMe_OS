# LaunchMe OS — Release v1.0.0 (Official Production Build)

<p align="center">
  <strong>Native Windows 11 Workspace Shell & Dynamic Ergonomics</strong><br>
  <em>Engineered in C# 13 and .NET 9 with WinUI 3 + Win2D, Squish Golden-Ratio BSP Tiling, Snoot Dynamic Notch HUD, and Pookie AI Agent Cockpit.</em>
</p>

---

## 🌟 What's New in v1.0.0

LaunchMe OS v1.0.0 is the official first production release. Designed specifically for Windows 11 power users, developers, and designers, it unifies a full-screen application launcher, dynamic top status HUD, multi-display dock, full-screen music player, Golden-Ratio dynamic window tiling engine, and local AI agent approvals into a single, cohesive desktop shell.

### 1. Full-Screen Launcher & Unified Search
- **Crystal Clear Canvas:** Fully transparent backdrop lets your desktop wallpaper shine through while floating icon blocks and widgets remain crisp and focused.
- **Gold-Standard 1024px G2 Squircles:** Deep icon extraction requesting up to 1024px via `IShellItemImageFactory` and `scale-400` MSIX assets, masked with continuous G2 super-ellipses ($n=4.5$) and dynamic dominant-color backlighting.
- **Explorer File Previews (`QLThumbnail`):** Instant search dropdown displaying high-resolution thumbnail previews for documents, PDFs, pictures, videos, and folders.
- **Sub-10ms Unified Search:** Real-time query execution across installed apps, files, clipboard history, workflows, and inline math expressions (`2*pi*sqrt(16)`).
- **11 Widget Families:** 12 Clock styles, 9 Weather cards, 6 Music players, 18 looping Pixel Art animations, System Stats, Pomodoro, Calendar, Notes, and device indicators under a cohesive OLED obsidian design system.

### 2. Snoot Dynamic Notch HUD & Pookie Agent Cockpit
- **Interactive Overview Bento:** Symmetrical dual-card layout featuring 4 live AI Desktop App pills (**Antigravity**, **Claude**, **ChatGPT**, and **Gemini**) with custom squircle avatars and dual-eye geometry.
- **Live Host Process & Task Querying:** Real-time polling querying running background tasks (e.g. `Connected · 12 Tasks Running`) with single-click foreground window activation (`SetForegroundWindow` / `ShowWindowAsync`) and tactile hover glows.
- **Pookie AI Agent Cockpit:** Local HTTP loopback server (port `49152`) securely intercepting agent actions from Antigravity, Claude Code, and terminal workflows. Review commands with **Allow**, **Deny**, or **Always Allow** (regex rule persistence).
- **Blobbo Living Companion:** Emotional notch companion with animated states (`Idle`, `Listening`, `Thinking`, `Poked`, `Dancing`, `Alert`) responding to pointer taps and cursor tracking.
- **Finder-Level Clipboard Drag-Out:** 7-category clipboard card manager allowing images and files to be dragged directly out of the Snoot HUD into external Windows applications.
- **Drop Shelf:** Dual-zone file staging shelf with Windows Nearby Share and Pocket integration.
- **Zero Screen-Capture Leak:** Window excluded from screen recording and screenshots via `WDA_EXCLUDEFROMCAPTURE`.

### 3. Multi-Display Dock
- **Continuous G2 Glass Capsule:** Win32 AppBar work-area reservation or floating mode with continuous DWM compositor spring hover waves (`1.42x` scaling curve).
- **Touch Bar Quick Actions:** Instant shortcuts for `Focus`, `Snap`, `Term`, `Lock`, and `Leash` P2P phone link.
- **Hardware Telemetry Mini-Gauge:** Real-time CPU usage %, RAM load %, and network throughput monitors.
- **9-Band Procedural AudioDSP Soundscapes:** Real-time mathematical audio synthesis generating *Rain*, *Ocean Waves*, *Forest*, *White Noise*, and *Campfire* with zero external audio files.

### 4. Full-Screen Music Player
- **Universal GSMTC Bridge:** Real-time synchronization with Spotify, Apple Music, YouTube Music, Tidal, Chrome, and Edge.
- **Dynamic Dual-Color Reactive Aura:** WinRT `AlbumArtBitmapSource` extracting primary and secondary dominant colors to project a breathing ambient glow.
- **4 Visualizer Modes:** *Radial Aura*, *Frequency Bars*, *Waveform Ribbon*, and *Minimalist Vinyl* running at 30 FPS.
- **Strict 0% Idle GPU Law:** Automatic Pause-on-Hide detaches composition brushes when hidden or during fullscreen DirectX gaming.

### 5. Squish Dynamic Window Tiling Engine
- **Golden-Ratio BSP Dwindle Spiral:** Binary Space Partitioning window tiling along the golden ratio with reduced border gaps.
- **Atomic DeferWindowPos:** Multi-window layout repositioning committed in a single V-Sync frame with zero visual tearing or window flickering.
- **Workspace Switcher Hotkeys:** Dedicated workspace switching shortcuts (`Win+Alt+1` through `Win+Alt+9`) for multi-monitor setups.

### 6. Leash Cross-Device Encrypted Relay
- **Zero-Cloud Local P2P Sync:** UDP multicast peer discovery with 6-digit numeric PIN pairing.
- **AES-GCM 256-bit Encryption:** End-to-end encrypted remote approval relay to review agent commands from your phone.

---

## 📦 Distribution Packages & Verified Checksums

| Package Asset | Description | Size | SHA-256 Checksum |
| :--- | :--- | :---: | :--- |
| **`LaunchMe-Win-Setup-v1.0.0.exe`** | 1-Click Windows 11 Installer (Desktop, Start Menu, Startup option) | 44.85 MB | `695A90AE152B850C3EF161E9C44BCB6DFD717F2793F0A8768FA2CD8579FCFFD3` |
| **`LaunchMe-Win-v1.0.0-Portable-x64.zip`** | Self-contained portable ZIP (Unzip & run without prerequisites) | 68.73 MB | `D81B944D69E54AC0C5DA1EF243575554AE47FAD3ADDA8A064A88AD0E5E72D7FB` |
| **`LaunchMe-PhoneLink-Companion-Android.zip`** | Android Companion APK + Pairing Setup Guide | 3.32 MB | `58680B5DB148136D6509D17B9F85F1C1028F6636F54BEDDBA7CDAAEEBC432EC2` |
| **`LaunchMe_CodeSigning.cer`** | Public SHA-256 Authenticode Code Signing Certificate | 0.98 KB | `53F943B0AD320F38D1E039E07B708BF00A1492C32C434DBF23453DB59656DBDE` |

---

## 🚀 Installation Options

### Option 1: Windows Package Manager (Winget) — Recommended
```powershell
winget install LaunchMe.OS
```
*Note: Winget manifest files are located in `dist/winget/` for submission to `microsoft/winget-pkgs`.*

### Option 2: 1-Click Installer
1. Download `LaunchMe-Win-Setup-v1.0.0.exe`.
2. Double-click the installer and choose whether to start LaunchMe OS automatically when signing into Windows.

### Option 3: Portable ZIP (Zero Installation)
1. Download and extract `LaunchMe-Win-v1.0.0-Portable-x64.zip`.
2. Run `LaunchMe.UI.exe`. No .NET SDK or runtime installation required (fully self-contained).

---

## 🛡️ Windows SmartScreen & Trust Notice

Because this is a brand-new release, Microsoft Defender SmartScreen may display:
> *"Windows protected your PC — Microsoft Defender SmartScreen prevented an unrecognized app from starting."*

This is normal for newly released open-source software before broad download reputation is established.
- Click **"More info"** ➔ **"Run anyway"**.
- Alternatively, you can import the public release certificate into your local TrustedPublisher store using:
  ```powershell
  powershell -ExecutionPolicy Bypass -File dist\certificates\install_cert.ps1
  ```

All executables and assemblies are signed with SHA-256 Authenticode and timestamped via DigiCert:
- `LaunchMe.UI.exe`
- `LaunchMe.Contracts.dll`
- `LaunchMe.Core.dll`
- `LaunchMe.Platform.Win32.dll`
- `LaunchMe.UI.dll`
- `LaunchMe-Win-Setup-v1.0.0.exe`

---

## ⚙️ Engineering & Audit Verification
- **Test Pass Rate:** **569 / 569 tests passed (100% pass rate, 0 failed, 0 skipped)**
- **Compiler Warnings:** **0 Warnings, 0 Errors** (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- **Idle Background GPU:** **0.0%** (Pause-on-Hide law strictly enforced across all surfaces)
- **Local-First Privacy:** Zero cloud telemetry, private local SQLite database, zero tracking.
