# LaunchMe OS (LaunchMe-Win) — Release v1.0.0

<p align="center">
  <strong>Native Windows 11 Workspace Manager & Full-Screen Shell</strong><br>
  <em>Achieving complete 1:1 feature parity with LaunchMe macOS v3.01.0, HyprMac dynamic tiling, and Coucou Mochi cockpit.</em>
</p>

---

## 🌟 What's New in v1.0.0

LaunchMe OS v1.0.0 is the first official production release, bringing the fluid ergonomics, design fidelity, and keyboard-centric workflows of macOS LaunchMe v3.01.0 to Windows 11, combined with the power of Hyprland dynamic tiling window management (**HyprWin**), the **Mochi Emotional Notch Companion & AI Agent Cockpit**, **Flow Push-to-Talk Voice Dictation**, and **AES-GCM 256-bit Encrypted Mobile Phone Link Relay**.

### 1. Full-Screen Launcher Grid & Unified Search (Surface 1)
- **Instant Desktop Summon:** Trigger via `Alt+Space`, configurable Hot Corners (`WH_MOUSE_LL` 150ms dwell), or 3/4/5-finger Precision Touchpad pinch/spread gestures.
- **Classic View & Free-Form Canvas:** Snap-to-grid app organization or customizable free-form layout.
- **Sub-10ms Unified Search:** Instant indexing across MSIX/Start Menu apps, local files, emojis, workflows, and inline math calculations (`2*pi*sqrt(16)`).
- **Complete 11-Family Widget Catalog:** Clock, Weather (Open-Meteo & NOAA), Music, System Stats, Pomodoro, Affirmations, Calendar, 18 Pixel Art ambient loops, Photo, Shape Photo (6 masks), and Pause-on-Hide GIF widgets.
- **Liquid Glass & Dynamic Wallpapers:** Custom Win2D acrylic backdrop with configurable tint opacity and NOAA solar time calculator.

### 2. Dynamic Notch HUD & Mochi Agent Cockpit (Surface 2)
- **Mochi Emotional Companion Engine:** Interactive companion with 8 animated states (`Idle`, `Listening`, `Thinking`, `Poked`, `Dizzy`, `Sleeping`, `Dancing`, `Alert`) and cursor-tracking pupil physics.
- **AI Agent Approval Cockpit:** Local HTTP loopback server (port `49152`) intercepting Claude Code, Antigravity, and CLI tool events. Allows high-risk commands to be reviewed and approved via **Allow**, **Deny**, or **Always Allow** (regex rule persistence).
- **Unified Git Diff Viewer:** Colorized inline syntax review displaying additions, deletions, chunk headers, and context lines without context switching.
- **Flow Voice Dictation:** System-wide push-to-talk dictation with filler-word suppression (`um`, `uh`, `like`), custom domain vocabulary correction, and a 9-band audio visualizer pill.
- **Pocket Shelf & Sharing Drop Zone:** Drag-and-drop file staging area with Windows Nearby Share integration.
- **Privacy & Game Auto-Hide:** Screen capture exclusion (`WDA_EXCLUDEFROMCAPTURE`) and DirectX 3D fullscreen auto-hide (`SHQueryUserNotificationState`).

### 3. Multi-Display Dock & Spaces (Surface 3)
- **Win32 AppBar Integration:** `SHAppBarMessage` work-area reservation or floating mode.
- **Multi-Monitor Dock Spaces:** Navigate independent dock arrangements per connected monitor with circular cycling.
- **System Stats Mini-Gauge:** Real-time CPU usage %, RAM load %, and network throughput (KB/s).
- **Procedural Ambient Sound Generator:** 5 built-in procedural soundscapes: *Rain*, *Ocean Waves*, *Forest*, *White Noise*, and *Cafe Stream*.
- **Wallpaper Engine Concentric Rings:** Ring image processor with configurable thickness, spacing, and saturation matrices.

### 4. Full-Screen Music Player (Surface 4)
- **Universal GSMTC Bridge:** Real-time synchronization with Spotify, Apple Music, YouTube Music, Tidal, Chrome, and Edge.
- **4 Reactive Visualizer Modes:** *Vinyl* (30 FPS spinning grooves), *FullScreenArt* (blurred cover art), *Particles*, and *GlowingBeats*.
- **Strict Pause-on-Hide GPU Law:** Zero background GPU utilization (`0.0%`) when hidden or minimized.

### 5. HyprWin Dynamic Tiling Engine (Surface 5)
- **Dwindle Golden-Ratio Spiral:** Binary Space Partitioning (BSP) tree dividing screens along the golden ratio with bounds clamping (`[0.15, 0.85]`).
- **Slot Memory:** Windows unminimizing automatically snap back into their exact prior split slot and ratio.
- **Atomic Win32 DeferWindowPos:** Multi-window updates committed in a single V-Sync frame with zero visual tearing.
- **Active Focus Glow & Acrylic Drop Previews:** Win2D focus adornments and semi-transparent ghost drag previews.

### 6. Cross-Device Phone Link & Encrypted Remote Relay (Surface 6)
- **Zero-Cloud Local P2P Sync:** UDP multicast peer discovery with temporary 6-digit numeric PIN challenge pairing.
- **AES-GCM 256-bit E2EE:** PBKDF2 (100,000 iterations), 96-bit unique nonces, and 128-bit authentication tags mathematically preventing replay and tampering.
- **Mobile Agent Approvals:** Forward CLI agent requests to your Android/iOS phone and approve/deny actions remotely.

---

## 📦 Distribution Packages & Checksums

| Package Asset | Description | Size | SHA-256 Checksum |
| :--- | :--- | :---: | :--- |
| **`LaunchMe-Win-Setup-v1.0.0.exe`** | 1-Click Windows 11 Installer (Desktop, Start Menu, `.launchmetheme` association) | 44.85 MB | `E3205367DEE5739BA052C5DC37BD06F1BE13F6406302FCC3C83A15931C3AAA4C` |
| **`LaunchMe-Win-v1.0.0-Portable-x64.zip`** | Self-contained portable ZIP (Unpack & run without installation) | 65.43 MB | `82E99B163E07C545147310603DAF7060D2B71448C3C2EF0D3F6CBC58EC8CFE2C` |
| **`LaunchMe-PhoneLink-Companion-Android.apk`** | Android Phone Link companion app (Android 11+) | 5.91 MB | `0E9F32D4040E552BDEE701163C74AB1ED1B87A4F17EA1FF36D075CE68C0D7161` |
| **`LaunchMe-PhoneLink-Companion-Android.zip`** | Android APK + Pairing Setup Guide | 3.32 MB | `58680B5DB148136D6509D17B9F85F1C1028F6636F54BEDDBA7CDAAEEBC432EC2` |
| **`LaunchMe_CodeSigning.cer`** | Public SHA-256 Authenticode Code Signing Certificate | 0.98 KB | `53F943B0AD320F38D1E039E07B708BF00A1492C32C434DBF23453DB59656DBDE` |

---

## 🔒 Code-Signing & Authenticode Integrity

All executable binaries and managed assemblies have been cryptographically signed using **SHA-256 Authenticode** and timestamped via **DigiCert SHA256 RSA4096 Timestamp Responder**:
- `LaunchMe.UI.exe`
- `LaunchMe.Contracts.dll`
- `LaunchMe.Core.dll`
- `LaunchMe.Platform.Win32.dll`
- `LaunchMe-Win-Setup-v1.0.0.exe`

To trust the open-source code-signing certificate on your local machine:
```powershell
powershell -ExecutionPolicy Bypass -File dist\certificates\install_cert.ps1
```

---

## ⚙️ Hardware Performance Verification
- **Test Pass Rate:** **455 / 455 Unit & Integration Tests Passed (100%)**
- **Compiler Warnings:** **0 Warnings, 0 Errors** (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- **Vulnerabilities:** **0 CVEs / 0 vulnerable NuGet packages**
- **Idle Background GPU:** **0.0%** (Pause-on-Hide law strictly enforced across all 5 surfaces)
- **Garbage Collection:** Enforced low-latency Workstation GC (`<ServerGarbageCollection>false</ServerGarbageCollection>`)
- **Cold Boot Time:** Sub-second instant launch via QuickJIT tiered compilation
