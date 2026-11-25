Add-Type -AssemblyName System.Drawing

$baseDir = "C:\Users\Redwan Ahmmed\.gemini\antigravity\brain\89c20678-0592-4161-88c5-7b793ab68dac"
$winScreenDir = "E:\LaunchMe OS\_analysis\coucou\windows\screenshots"
$frameDir = "$baseDir\videos\win11_frames"
$tempDir = "$baseDir\videos\temp_win11"
$outDir = "$baseDir\videos"

if (!(Test-Path $frameDir)) { New-Item -ItemType Directory -Path $frameDir -Force }
if (!(Test-Path $tempDir)) { New-Item -ItemType Directory -Path $tempDir -Force }

# Shared Brushes
$whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$cyanBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(56, 189, 248))
$emeraldBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(52, 211, 153))
$purpleBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(168, 85, 247))
$pinkBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(236, 72, 153))

# Helper to draw Windows 11 Taskbar
function Draw-Windows11Taskbar($g, $w, $h) {
    # Taskbar Background (Mica / Dark Translucent)
    $tbHeight = 44
    $tbY = $h - $tbHeight
    $tbBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 24, 24, 32))
    $g.FillRectangle($tbBrush, 0, $tbY, $w, $tbHeight)
    
    # Taskbar top line
    $tbPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(40, 255, 255, 255), 1)
    $g.DrawLine($tbPen, 0, $tbY, $w, $tbY)

    # Centered Windows 11 Taskbar Icons
    $center = $w / 2
    $startX = $center - 130
    $iconY = $tbY + 10

    # 1. Windows 11 Logo (4 blue tiles)
    $winBlue = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(0, 164, 239))
    $g.FillRectangle($winBlue, $startX, $iconY, 10, 10)
    $g.FillRectangle($winBlue, $startX + 12, $iconY, 10, 10)
    $g.FillRectangle($winBlue, $startX, $iconY + 12, 10, 10)
    $g.FillRectangle($winBlue, $startX + 12, $iconY + 12, 10, 10)

    # 2. Search Icon
    $searchBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 200, 220))
    $fontEmoji = New-Object System.Drawing.Font ("Segoe UI Emoji", 12)
    $g.DrawString("🔍", $fontEmoji, $searchBrush, $startX + 35, $iconY)

    # 3. Task View
    $g.DrawString("◫", $fontEmoji, $searchBrush, $startX + 70, $iconY)

    # 4. File Explorer
    $yellowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 205, 75))
    $g.DrawString("📁", $fontEmoji, $yellowBrush, $startX + 105, $iconY)

    # 5. Microsoft Edge
    $edgeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(0, 120, 215))
    $g.DrawString("🌐", $fontEmoji, $edgeBrush, $startX + 140, $iconY)

    # 6. Windows Terminal
    $termBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(60, 60, 60))
    $g.DrawString("💻", $fontEmoji, $searchBrush, $startX + 175, $iconY)

    # 7. LaunchMe Icon (Active)
    $lmBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(139, 92, 246))
    $g.DrawString("🚀", $fontEmoji, $lmBrush, $startX + 210, $iconY)
    
    # Active indicator dot under LaunchMe
    $dotBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(59, 130, 246))
    $g.FillEllipse($dotBrush, $startX + 218, $h - 5, 6, 3)

    # System Tray (Right side)
    $trayFont = New-Object System.Drawing.Font ("Segoe UI Variable Display", 9)
    $trayBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 210, 220))
    $g.DrawString("ENG   📶   🔊   100%   12:00 PM", $trayFont, $trayBrush, $w - 220, $tbY + 14)
}

# Helper to draw Windows 11 Dynamic Notch HUD (Top-Center)
function Draw-Windows11Notch($g, $w, $expanded = $false) {
    $notchW = if ($expanded) { 460 } else { 260 }
    $notchH = if ($expanded) { 160 } else { 34 }
    $notchX = ($w - $notchW) / 2
    $notchY = 0

    # Notch Body (Deep black with subtle border)
    $notchBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 12, 14, 20))
    $rect = New-Object System.Drawing.Rectangle $notchX, $notchY, $notchW, $notchH
    $g.FillRectangle($notchBrush, $rect)

    $notchPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(50, 255, 255, 255), 1)
    $g.DrawRectangle($notchPen, $rect)

    $fontText = New-Object System.Drawing.Font ("Segoe UI Variable Display", 9, [System.Drawing.FontStyle]::Bold)
    $fontMuted = New-Object System.Drawing.Font ("Segoe UI Variable Display", 8)
    $whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $cyanBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(56, 189, 248))

    if (-not $expanded) {
        # Compact Notch: Mochi Icon + Weather + Now Playing
        $g.DrawString("🐾", $fontText, $whiteBrush, $notchX + 12, 8)
        $g.DrawString("21°C", $fontMuted, $whiteBrush, $notchX + 38, 9)
        $g.DrawString("Taylor Swift - Cruel Summer", $fontText, $cyanBrush, $notchX + 80, 9)
        $g.DrawString("98% 🔋", $fontMuted, $whiteBrush, $notchX + $notchW - 55, 9)
    } else {
        # Expanded Notch Drawer: Mochi + Voice Dictation + Pocket
        $g.DrawString("🐾 Mochi Cockpit", $fontText, $whiteBrush, $notchX + 16, 10)
        $g.DrawString("Flow Voice: Push-to-Talk active (9-band audio levels)", $fontMuted, $cyanBrush, $notchX + 16, 32)
        
        # Audio Waveform bars
        $barBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(59, 130, 246))
        for ($i = 0; $i -lt 9; $i++) {
            $bh = 8 + ($i % 4) * 6
            $g.FillRectangle($barBrush, $notchX + 16 + ($i * 8), 54 + (24 - $bh), 5, $bh)
        }

        # Pocket drop zone
        $pocketRect = New-Object System.Drawing.Rectangle ($notchX + 120), 54, ($notchW - 136), 90
        $pocketBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(40, 255, 255, 255))
        $g.FillRectangle($pocketBrush, $pocketRect)
        $g.DrawString("📂 Pocket Shelf: Drag files here to share", $fontMuted, $whiteBrush, ($notchX + 130), 88)
    }
}

# Helper to draw Windows 11 Window Frame (with top-right — 🗖 ✕ controls)
function Draw-Windows11Window($g, $x, $y, $w, $h, $title, $active = $false) {
    # Window Body
    $winBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(240, 30, 30, 40))
    $g.FillRectangle($winBrush, $x, $y, $w, $h)

    # Titlebar
    $tbBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 20, 22, 28))
    $g.FillRectangle($tbBrush, $x, $y, $w, 32)

    # Border & Active Focus Glow
    $borderPen = if ($active) {
        New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 56, 189, 248), 2)
    } else {
        New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(60, 255, 255, 255), 1)
    }
    $g.DrawRectangle($borderPen, $x, $y, $w, $h)

    # Title Text (Segoe UI)
    $fontTitle = New-Object System.Drawing.Font ("Segoe UI Variable Text", 9)
    $whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 230, 230))
    $g.DrawString($title, $fontTitle, $whiteBrush, $x + 12, $y + 8)

    # Standard Windows 11 Window Controls on TOP-RIGHT: — 🗖 ✕
    $ctrlFont = New-Object System.Drawing.Font ("Segoe UI", 9)
    $g.DrawString("—", $ctrlFont, $whiteBrush, $x + $w - 65, $y + 7)
    $g.DrawString("🗖", $ctrlFont, $whiteBrush, $x + $w - 42, $y + 8)
    $g.DrawString("✕", $ctrlFont, $whiteBrush, $x + $w - 20, $y + 8)
}

# --- FRAME 1: Windows 11 Desktop with Dynamic Notch & LaunchMe Dock ---
Write-Host "Rendering Frame 1: Windows 11 Desktop with Dynamic Notch & Dock..."
$bmp1 = New-Object System.Drawing.Bitmap 1280, 720
$g1 = [System.Drawing.Graphics]::FromImage($bmp1)
$g1.Clear([System.Drawing.Color]::FromArgb(12, 18, 30)) # Windows 11 Dark Blue

# Windows 11 Bloom Ambient Wallpaper glow
$bloomBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (
    (New-Object System.Drawing.Point 0, 0),
    (New-Object System.Drawing.Point 1280, 720),
    [System.Drawing.Color]::FromArgb(20, 35, 75),
    [System.Drawing.Color]::FromArgb(8, 12, 24)
)
$g1.FillRectangle($bloomBrush, 0, 0, 1280, 720)

# Windows 11 Dynamic Notch (Top)
Draw-Windows11Notch $g1 1280 $false

# LaunchMe Multi-Display Dock (Above Taskbar)
$dockW = 600
$dockH = 52
$dockX = (1280 - $dockW) / 2
$dockY = 720 - 44 - $dockH - 12
$dockBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 25, 30, 45))
$g1.FillRectangle($dockBrush, $dockX, $dockY, $dockW, $dockH)
$dockPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(60, 255, 255, 255), 1)
$g1.DrawRectangle($dockPen, $dockX, $dockY, $dockW, $dockH)

$fontDock = New-Object System.Drawing.Font ("Segoe UI Variable Display", 9, [System.Drawing.FontStyle]::Bold)
$fontStats = New-Object System.Drawing.Font ("Segoe UI Variable Text", 8)
$whiteBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$emeraldBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(52, 211, 153))

$g1.DrawString("LaunchMe Multi-Display Dock", $fontDock, $whiteBrush, $dockX + 16, $dockY + 10)
$g1.DrawString("Dock Space 1/3  •  CPU: 14%  RAM: 4.8 GB  NET: 1.2 MB/s  •  🌧️ Rain Soundscape", $fontStats, $emeraldBrush, $dockX + 16, $dockY + 30)

# Windows 11 Taskbar (Bottom)
Draw-Windows11Taskbar $g1 1280 720

$bmp1.Save("$frameDir\f1_win11_desktop.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g1.Dispose()
$bmp1.Dispose()

# --- FRAME 2: Windows 11 Full-Screen Launcher Grid (Alt+Space) ---
Write-Host "Rendering Frame 2: Windows 11 Launcher Grid & Liquid Glass..."
$bmp2 = New-Object System.Drawing.Bitmap 1280, 720
$g2 = [System.Drawing.Graphics]::FromImage($bmp2)
# Desktop Acrylic Backdrop
$g2.Clear([System.Drawing.Color]::FromArgb(235, 15, 20, 32))

# Dynamic Notch on top
Draw-Windows11Notch $g2 1280 $false

# Centered Search Bar
$searchW = 560
$searchH = 46
$searchX = (1280 - $searchW) / 2
$searchY = 70
$sbBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(250, 28, 34, 48))
$g2.FillRectangle($sbBrush, $searchX, $searchY, $searchW, $searchH)
$sbPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(100, 59, 130, 246), 2)
$g2.DrawRectangle($sbPen, $searchX, $searchY, $searchW, $searchH)

$fontSearch = New-Object System.Drawing.Font ("Segoe UI Variable Display", 11)
$g2.DrawString("🔍  Search Windows apps, files, math, emojis (Alt+Space)...", $fontSearch, $whiteBrush, $searchX + 16, $searchY + 12)

# 11 Widget Cards Grid
$widgetW = 180
$widgetH = 110
$widgetTitles = @("⏰ World Clock", "🌦️ NOAA Weather", "🎵 GSMTC Player", "📊 System Stats", "⏳ Pomodoro 25:00", "🎨 Pixel Art #04")
for ($col = 0; $col -lt 3; $col++) {
    for ($row = 0; $row -lt 2; $row++) {
        $idx = $row * 3 + $col
        $wx = 200 + ($col * 300)
        $wy = 145 + ($row * 135)
        $wcardBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 30, 38, 55))
        $g2.FillRectangle($wcardBrush, $wx, $wy, 270, 115)
        $g2.DrawRectangle($dockPen, $wx, $wy, 270, 115)
        $g2.DrawString($widgetTitles[$idx], $fontDock, $whiteBrush, $wx + 14, $wy + 12)
    }
}

# Windows Apps Grid below
$fontApp = New-Object System.Drawing.Font ("Segoe UI Variable Display", 9)
$appNames = @("💻 Terminal", "📝 VS Code", "📁 Explorer", "🌐 Edge", "⚙️ Settings", "🎧 Spotify", "🧮 Calculator", "📱 Phone Link")
for ($i = 0; $i -lt 8; $i++) {
    $ax = 200 + ($i * 115)
    $ay = 435
    $appCard = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 24, 30, 42))
    $g2.FillRectangle($appCard, $ax, $ay, 95, 80)
    $g2.DrawRectangle($dockPen, $ax, $ay, 95, 80)
    $g2.DrawString($appNames[$i], $fontApp, $whiteBrush, $ax + 8, $ay + 30)
}

Draw-Windows11Taskbar $g2 1280 720
$bmp2.Save("$frameDir\f2_win11_launcher_grid.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g2.Dispose()
$bmp2.Dispose()

# --- FRAME 3: Windows 11 HyprWin Dynamic Tiling (Dwindle Spiral) ---
Write-Host "Rendering Frame 3: Windows 11 HyprWin Dynamic Tiling Engine..."
$bmp3 = New-Object System.Drawing.Bitmap 1280, 720
$g3 = [System.Drawing.Graphics]::FromImage($bmp3)
$g3.Clear([System.Drawing.Color]::FromArgb(10, 14, 24))

Draw-Windows11Notch $g3 1280 $false

# Tiled Windows (Golden Ratio BSP Layout)
# Left 50%: Visual Studio Code (Active Focus with Cyan Glow)
Draw-Windows11Window $g3 16 48 610 610 "Visual Studio Code - LaunchMe OS" $true

# Inside VS Code: code snippet
$fontCode = New-Object System.Drawing.Font ("Cascadia Code", 8)
$g3.DrawString("// LaunchMe.Platform.Win32 - DeferWindowPos Multi-Window Tiling", $fontCode, $whiteBrush, 30, 95)
$g3.DrawString("hdwp = BeginDeferWindowPos(windowCount);", $fontCode, $emeraldBrush, 30, 115)
$g3.DrawString("DeferWindowPos(hdwp, hwnd, HWND_TOP, x, y, cx, cy, SWP_NOACTIVATE);", $fontCode, $whiteBrush, 30, 135)
$g3.DrawString("EndDeferWindowPos(hdwp); // Atomic V-Sync commit", $fontCode, $emeraldBrush, 30, 155)

# Top Right 25%: Windows Terminal
Draw-Windows11Window $g3 642 48 622 295 "Windows Terminal (PowerShell)" $false
$g3.DrawString("PS E:\LaunchMe OS> dotnet test --no-build", $fontCode, $whiteBrush, 656, 95)
$g3.DrawString("Passed! - Failed: 0, Passed: 455, Total: 455", $fontCode, $emeraldBrush, 656, 120)

# Bottom Right 25%: Microsoft Edge
Draw-Windows11Window $g3 642 355 622 303 "Microsoft Edge - Windows 11 Developer Portal" $false
$g3.DrawString("https://github.com/redwan2003-bot/LaunchMe_OS", $fontCode, $cyanBrush, 656, 400)

Draw-Windows11Taskbar $g3 1280 720
$bmp3.Save("$frameDir\f3_win11_hyprwin_tiling.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g3.Dispose()
$bmp3.Dispose()

# --- FRAME 4: Windows 11 AI Agent Approval Cockpit (using approval.png) ---
Write-Host "Rendering Frame 4: Windows 11 AI Agent Approval Cockpit..."
$bmp4 = New-Object System.Drawing.Bitmap 1280, 720
$g4 = [System.Drawing.Graphics]::FromImage($bmp4)
$g4.Clear([System.Drawing.Color]::FromArgb(15, 20, 32))

Draw-Windows11Notch $g4 1280 $true

# Center the actual Windows approval.png screenshot inside Windows 11
$approvalImgPath = "$winScreenDir\approval.png"
if (Test-Path $approvalImgPath) {
    $srcImg = [System.Drawing.Image]::FromFile($approvalImgPath)
    $appW = 760
    $appH = [int](($srcImg.Height / $srcImg.Width) * $appW)
    $appX = (1280 - $appW) / 2
    $appY = 175
    $g4.DrawImage($srcImg, $appX, $appY, $appW, $appH)
    $srcImg.Dispose()
}

Draw-Windows11Taskbar $g4 1280 720
$bmp4.Save("$frameDir\f4_win11_approval_cockpit.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g4.Dispose()
$bmp4.Dispose()

# --- FRAME 5: Windows 11 Phone Link P2P Relay (using settings.png) ---
Write-Host "Rendering Frame 5: Windows 11 Phone Link P2P Relay..."
$bmp5 = New-Object System.Drawing.Bitmap 1280, 720
$g5 = [System.Drawing.Graphics]::FromImage($bmp5)
$g5.Clear([System.Drawing.Color]::FromArgb(15, 20, 32))

Draw-Windows11Notch $g5 1280 $false

# Center real Windows settings.png
$settingsImgPath = "$winScreenDir\settings.png"
if (Test-Path $settingsImgPath) {
    $srcImg = [System.Drawing.Image]::FromFile($settingsImgPath)
    $setW = 800
    $setH = [int](($srcImg.Height / $srcImg.Width) * $setW)
    $setX = (1280 - $setW) / 2
    $setY = 90
    $g5.DrawImage($srcImg, $setX, $setY, $setW, $setH)
    $srcImg.Dispose()
}

Draw-Windows11Taskbar $g5 1280 720
$bmp5.Save("$frameDir\f5_win11_phonelink_settings.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g5.Dispose()
$bmp5.Dispose()

# --- FRAME 6: Windows 11 Full-Screen Music Player (GSMTC) ---
Write-Host "Rendering Frame 6: Windows 11 Full-Screen Music Player..."
$bmp6 = New-Object System.Drawing.Bitmap 1280, 720
$g6 = [System.Drawing.Graphics]::FromImage($bmp6)
$g6.Clear([System.Drawing.Color]::FromArgb(14, 18, 28))

Draw-Windows11Notch $g6 1280 $false

# Vinyl Record Visualizer (Left side)
$vinylPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(40, 255, 255, 255), 1)
$vinylFill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(250, 18, 20, 26))
$g6.FillEllipse($vinylFill, 150, 140, 380, 380)
$g6.DrawEllipse($vinylPen, 150, 140, 380, 380)
# Grooves
for ($r = 40; $r -lt 180; $r += 12) {
    $g6.DrawEllipse($vinylPen, (340 - $r), (330 - $r), ($r * 2), ($r * 2))
}
# Vinyl Center Label
$g6.FillEllipse($pinkBrush, 290, 280, 100, 100)
$fontVinyl = New-Object System.Drawing.Font ("Segoe UI Variable Display", 11, [System.Drawing.FontStyle]::Bold)
$g6.DrawString("🎵 GSMTC", $fontVinyl, $whiteBrush, 305, 318)

# Right Side: Windows GSMTC Metadata & Controls
$fontTrack = New-Object System.Drawing.Font ("Segoe UI Variable Display", 22, [System.Drawing.FontStyle]::Bold)
$fontArtist = New-Object System.Drawing.Font ("Segoe UI Variable Text", 14)
$g6.DrawString("Cruel Summer", $fontTrack, $whiteBrush, 580, 220)
$g6.DrawString("Taylor Swift  •  Lover (2019)", $fontArtist, $cyanBrush, 580, 265)

# Seek bar
$sbRect = New-Object System.Drawing.Rectangle 580, 320, 480, 6
$sbFill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(60, 255, 255, 255))
$sbProg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(59, 130, 246))
$g6.FillRectangle($sbFill, $sbRect)
$g6.FillRectangle($sbProg, 580, 320, 220, 6)

$fontTime = New-Object System.Drawing.Font ("Segoe UI Variable Text", 9)
$g6.DrawString("1:34", $fontTime, $whiteBrush, 580, 335)
$g6.DrawString("2:58", $fontTime, $whiteBrush, 1030, 335)

# Media Controls
$fontCtrls = New-Object System.Drawing.Font ("Segoe UI Variable Display", 18)
$g6.DrawString("⏮     ⏸     ⏭", $fontCtrls, $whiteBrush, 740, 370)

# Mode Selector
$fontModes = New-Object System.Drawing.Font ("Segoe UI Variable Text", 9)
$g6.DrawString("Visualizer Mode: [ Vinyl ]   FullScreenArt   Particles   GlowingBeats", $fontModes, $emeraldBrush, 580, 440)
$g6.DrawString("⚡ Pause-on-Hide Law: 0.0% GPU load when hidden or minimized", $fontTime, $cyanBrush, 580, 470)

Draw-Windows11Taskbar $g6 1280 720
$bmp6.Save("$frameDir\f6_win11_music_player.png", [System.Drawing.Imaging.ImageFormat]::Png)
$g6.Dispose()
$bmp6.Dispose()

Write-Host "All 6 Pure Windows 11 visual frames generated in $frameDir" -ForegroundColor Green

# --- RENDER 30 FPS MP4 & WEBM VIDEO USING WINDOWS 11 FRAMES ---
Write-Host "Compiling 30 FPS Windows 11 Video Tour with FFmpeg..." -ForegroundColor Cyan

$vf = "scale=1280:720,format=yuv420p"

# 1. Desktop & Dock (7s)
ffmpeg -y -loop 1 -t 7 -i "$frameDir\f1_win11_desktop.png" -vf "$vf,fade=t=in:st=0:d=0.5" -r 30 -c:v libx264 -preset fast "$tempDir\w1.mp4"
# 2. Launcher Grid (8s)
ffmpeg -y -loop 1 -t 8 -i "$frameDir\f2_win11_launcher_grid.png" -vf "$vf" -r 30 -c:v libx264 -preset fast "$tempDir\w2.mp4"
# 3. Dynamic Notch & Approvals (8s)
ffmpeg -y -loop 1 -t 8 -i "$frameDir\f4_win11_approval_cockpit.png" -vf "$vf" -r 30 -c:v libx264 -preset fast "$tempDir\w3.mp4"
# 4. HyprWin Dynamic Tiling (9s)
ffmpeg -y -loop 1 -t 9 -i "$frameDir\f3_win11_hyprwin_tiling.png" -vf "$vf" -r 30 -c:v libx264 -preset fast "$tempDir\w4.mp4"
# 5. Full-Screen Music Player (8s)
ffmpeg -y -loop 1 -t 8 -i "$frameDir\f6_win11_music_player.png" -vf "$vf" -r 30 -c:v libx264 -preset fast "$tempDir\w5.mp4"
# 6. Phone Link Settings & Outro (8s)
ffmpeg -y -loop 1 -t 8 -i "$frameDir\f5_win11_phonelink_settings.png" -vf "$vf,fade=t=out:st=7:d=1" -r 30 -c:v libx264 -preset fast "$tempDir\w6.mp4"

# Concat
$concatFile = "$tempDir\win11_concat.txt"
@(
    "file '$tempDir\w1.mp4'",
    "file '$tempDir\w2.mp4'",
    "file '$tempDir\w3.mp4'",
    "file '$tempDir\w4.mp4'",
    "file '$tempDir\w5.mp4'",
    "file '$tempDir\w6.mp4'"
) | Out-File -FilePath $concatFile -Encoding ascii

$win11Mp4 = "$outDir\launchme_os_windows11_tour_30fps.mp4"
$win11Webm = "$outDir\launchme_os_windows11_tour_30fps.webm"

ffmpeg -y -f concat -safe 0 -i "$concatFile" -c copy "$win11Mp4"
ffmpeg -y -i "$win11Mp4" -c:v libvpx-vp9 -b:v 1500k -r 30 "$win11Webm"

# Also copy to dist\videos
Copy-Item "$win11Mp4" "E:\LaunchMe OS\dist\videos\" -Force
Copy-Item "$win11Webm" "E:\LaunchMe OS\dist\videos\" -Force

Write-Host "Pure Windows 11 30 FPS Video Tour ready!" -ForegroundColor Green
Write-Host "MP4:  $win11Mp4"
Write-Host "WebM: $win11Webm"

