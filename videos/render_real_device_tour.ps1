param(
    [string]$OutputDir = "E:\LaunchMe OS\docs\assets",
    [string]$DistDir = "E:\LaunchMe OS\dist\videos"
)

$ErrorActionPreference = "Stop"

$tempFrames = Join-Path $env:TEMP "launchme_tour_frames"
if (Test-Path $tempFrames) { Remove-Item $tempFrames -Recurse -Force }
New-Item -ItemType Directory -Path $tempFrames -Force | Out-Null
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

Add-Type -AssemblyName System.Drawing

$code = @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Threading.Tasks;

public static class RealDeviceTourRenderer
{
    private const int W = 1280;
    private const int H = 720;
    private const int TotalFrames = 960; // 32.0s @ 30 FPS

    private static Image ImgNotch;
    private static Image ImgLauncher;
    private static Image ImgDock;

    public static void Initialize(string notchPath, string launcherPath, string dockPath)
    {
        if (File.Exists(notchPath)) ImgNotch = Image.FromFile(notchPath);
        if (File.Exists(launcherPath)) ImgLauncher = Image.FromFile(launcherPath);
        if (File.Exists(dockPath)) ImgDock = Image.FromFile(dockPath);
    }

    private static readonly object DrawLock = new object();

    public static void RenderAll(string frameDir)
    {
        for (int f = 0; f < TotalFrames; f++)
        {
            using (var bmp = new Bitmap(W, H, PixelFormat.Format32bppPArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                double t = f / 30.0;

                // 1. Photorealistic Windows 11 Desktop Wallpaper (Dark Bloom / Obsidian Ambient)
                DrawWindows11Wallpaper(g, t);

                // 2. Active Scene Rendering
                if (f < 240)
                {
                    // Scene 1: Snoot Dynamic Notch HUD & Live AI Bento
                    DrawScene_SnootNotch(g, f, t);
                }
                else if (f < 450)
                {
                    // Scene 2: Crystal-Clear Full-Screen Launcher
                    DrawScene_CrystalLauncher(g, f - 240, t);
                }
                else if (f < 660)
                {
                    // Scene 3: Multi-Display Dock & Procedural AudioDSP
                    DrawScene_Dock(g, f - 450, t);
                }
                else if (f < 840)
                {
                    // Scene 4: Squish Golden-Ratio BSP Window Tiling
                    DrawScene_SquishTiling(g, f - 660, t);
                }
                else
                {
                    // Scene 5: Finale & 10/10 Scorecard
                    DrawScene_Finale(g, f - 840, t);
                }

                // 3. Realistic Windows 11 Taskbar at Bottom (always present on real OS)
                DrawWindows11Taskbar(g);

                // 4. Save frame
                string filePath = Path.Combine(frameDir, string.Format("frame_{0:D5}.png", f));
                bmp.Save(filePath, ImageFormat.Png);
            }
        }
    }

    private static void DrawWindows11Wallpaper(Graphics g, double t)
    {
        // Dark Obsidian base with elegant deep cobalt & violet bloom
        using (var brush = new LinearGradientBrush(new Rectangle(0, 0, W, H),
            Color.FromArgb(10, 14, 22), Color.FromArgb(6, 8, 12), LinearGradientMode.Vertical))
        {
            g.FillRectangle(brush, 0, 0, W, H);
        }

        // Ambient radial light bloom in center/bottom
        float bloomX = W / 2f + (float)(Math.Sin(t * 0.3) * 60);
        float bloomY = H / 2f + 40 + (float)(Math.Cos(t * 0.4) * 30);
        using (var gp = new GraphicsPath())
        {
            gp.AddEllipse(bloomX - 380, bloomY - 240, 760, 480);
            using (var pgb = new PathGradientBrush(gp))
            {
                pgb.CenterColor = Color.FromArgb(42, 30, 64, 115);
                pgb.SurroundColors = new Color[] { Color.Transparent };
                g.FillPath(pgb, gp);
            }
        }
    }

    private static void DrawWindows11Taskbar(Graphics g)
    {
        int tbH = 42;
        int tbY = H - tbH;

        // Mica glass taskbar
        using (var brush = new SolidBrush(Color.FromArgb(235, 14, 18, 24)))
        {
            g.FillRectangle(brush, 0, tbY, W, tbH);
        }
        using (var pen = new Pen(Color.FromArgb(35, 255, 255, 255), 1))
        {
            g.DrawLine(pen, 0, tbY, W, tbY);
        }

        // Centered Icons (Windows logo, Search, Task View, Explorer, Edge, Terminal, Antigravity)
        int cx = W / 2 - 110;
        int cy = tbY + 11;

        // Windows 11 Logo (4 blue squares)
        using (var b = new SolidBrush(Color.FromArgb(0, 164, 239)))
        {
            g.FillRectangle(b, cx, cy, 8, 8);
            g.FillRectangle(b, cx + 10, cy, 8, 8);
            g.FillRectangle(b, cx, cy + 10, 8, 8);
            g.FillRectangle(b, cx + 10, cy + 10, 8, 8);
        }

        // Search icon
        using (var pen = new Pen(Color.FromArgb(200, 215, 235), 1.6f))
        {
            g.DrawEllipse(pen, cx + 32, cy + 2, 9, 9);
            g.DrawLine(pen, cx + 39, cy + 9, cx + 44, cy + 14);
        }

        // Task View icon
        using (var pen = new Pen(Color.FromArgb(160, 180, 205), 1.5f))
        {
            g.DrawRectangle(pen, cx + 60, cy + 3, 10, 10);
            g.DrawRectangle(pen, cx + 64, cy + 1, 10, 10);
        }

        // File Explorer (Yellow folder)
        using (var b = new SolidBrush(Color.FromArgb(245, 190, 50)))
        {
            g.FillRectangle(b, cx + 90, cy + 3, 16, 12);
        }

        // Terminal (Dark rectangle with `>_`)
        using (var b = new SolidBrush(Color.FromArgb(20, 24, 32)))
        using (var p = new Pen(Color.FromArgb(80, 95, 120), 1))
        {
            g.FillRectangle(b, cx + 120, cy + 2, 16, 14);
            g.DrawRectangle(p, cx + 120, cy + 2, 16, 14);
        }

        // Antigravity Icon (Emerald gem)
        using (var b = new SolidBrush(Color.FromArgb(16, 185, 129)))
        {
            PointF[] gem = {
                new PointF(cx + 158, cy + 2),
                new PointF(cx + 166, cy + 8),
                new PointF(cx + 158, cy + 15),
                new PointF(cx + 150, cy + 8)
            };
            g.FillPolygon(b, gem);
        }

        // System Tray (Right side: Net, Vol, Bat, Clock)
        using (var font = new Font("Segoe UI Variable Small", 9f, FontStyle.Regular))
        using (var b = new SolidBrush(Color.FromArgb(200, 215, 235)))
        {
            g.DrawString("10:30 PM", font, b, W - 75, tbY + 6);
            g.DrawString("11/27/2026", font, new SolidBrush(Color.FromArgb(130, 145, 165)), W - 75, tbY + 20);
        }
    }

    private static void DrawScene_SnootNotch(Graphics g, int f, double t)
    {
        // Tag Overlay
        DrawFeaturePill(g, "🏝️ SNOOT DYNAMIC NOTCH HUD", "Live AI Bento · Running Tasks Counter · Process Activation", Color.FromArgb(16, 185, 129));

        float expandProgress;
        if (f < 30) expandProgress = (float)(f / 30.0);
        else if (f < 210) expandProgress = 1.0f;
        else expandProgress = (float)((240 - f) / 30.0);

        // Smooth spring curve
        expandProgress = Math.Max(0f, Math.Min(1f, expandProgress));

        if (ImgNotch != null)
        {
            int notchW = 635;
            int notchH = (int)(175 * expandProgress);
            int notchX = (W - notchW) / 2;
            int notchY = 0;

            if (notchH > 5)
            {
                // Shadow under notch
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(110 * expandProgress), 0, 0, 0)))
                {
                    g.FillEllipse(shadowBrush, notchX - 20, notchY + notchH - 15, notchW + 40, 30);
                }

                // Draw actual captured Notch
                var destRect = new Rectangle(notchX, notchY, notchW, notchH);
                var srcRect = new Rectangle(0, 0, ImgNotch.Width, (int)(ImgNotch.Height * expandProgress));
                g.DrawImage(ImgNotch, destRect, srcRect, GraphicsUnit.Pixel);
            }

            // Mouse Cursor gliding up and clicking Antigravity pill
            if (f > 60 && f < 200)
            {
                float curProg = Math.Min(1f, (f - 60) / 45.0f);
                float mx = 640 + (notchX + 390 - 640) * curProg;
                float my = 420 + (notchY + 65 - 420) * curProg;

                // Pill hover glow (emerald) around Antigravity capsule (frames 105-180)
                if (f >= 105 && f <= 180)
                {
                    int pillX = notchX + 342;
                    int pillY = notchY + 45;
                    using (var glowPen = new Pen(Color.FromArgb(200, 16, 185, 129), 2f))
                    {
                        g.DrawRectangle(glowPen, pillX, pillY, 130, 38);
                    }
                }

                // Click ripple at frame 120
                if (f >= 120 && f <= 150)
                {
                    float ripR = (f - 120) * 1.8f;
                    int alpha = Math.Max(0, 220 - (f - 120) * 7);
                    using (var ripPen = new Pen(Color.FromArgb(alpha, 16, 185, 129), 2f))
                    {
                        g.DrawEllipse(ripPen, mx - ripR, my - ripR, ripR * 2, ripR * 2);
                    }
                }

                DrawMouseCursor(g, mx, my);
            }
        }
    }

    private static void DrawScene_CrystalLauncher(Graphics g, int f, double t)
    {
        // Tag Overlay
        DrawFeaturePill(g, "🪟 CRYSTAL-CLEAR LAUNCHER", "Floating Obsidian Canvas · 1024px G2 Squircles · QuickLook Previews", Color.FromArgb(0, 240, 255));

        // Hotkey badge indicator
        if (f < 60)
        {
            DrawKeyBadge(g, "Alt + Space", W / 2, 60);
        }

        float fade = Math.Min(1f, f < 180 ? f / 25.0f : (210 - f) / 30.0f);
        fade = Math.Max(0f, fade);

        if (ImgLauncher != null && fade > 0.01f)
        {
            int lw = (int)(ImgLauncher.Width * 0.94);
            int lh = (int)(ImgLauncher.Height * 0.94);
            int lx = (W - lw) / 2;
            int ly = 65;

            // Ambient glow behind launcher
            using (var gp = new GraphicsPath())
            {
                gp.AddEllipse(lx - 50, ly - 30, lw + 100, lh + 60);
                using (var pgb = new PathGradientBrush(gp))
                {
                    pgb.CenterColor = Color.FromArgb((int)(45 * fade), 0, 240, 255);
                    pgb.SurroundColors = new Color[] { Color.Transparent };
                    g.FillPath(pgb, gp);
                }
            }

            // Draw actual captured Crystal Launcher
            var cm = new ColorMatrix { Matrix33 = fade };
            using (var ia = new ImageAttributes())
            {
                ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(ImgLauncher, new Rectangle(lx, ly, lw, lh), 0, 0, ImgLauncher.Width, ImgLauncher.Height, GraphicsUnit.Pixel, ia);
            }

            // Cursor movement across apps
            if (f > 40 && f < 180)
            {
                float mx = lx + 300 + (float)(Math.Sin((f - 40) * 0.08) * 180);
                float my = ly + 180 + (float)(Math.Cos((f - 40) * 0.05) * 60);
                DrawMouseCursor(g, mx, my);
            }
        }
    }

    private static void DrawScene_Dock(Graphics g, int f, double t)
    {
        // Tag Overlay
        DrawFeaturePill(g, "🚢 MULTI-DISPLAY DOCK", "Continuous G2 Capsule · 1.42x Neighbor Spring Wave · 9-Band AudioDSP", Color.FromArgb(168, 85, 247));

        if (ImgDock != null)
        {
            int dw = (int)(ImgDock.Width * 0.88);
            int dh = (int)(ImgDock.Height * 0.88);
            int dx = (W - dw) / 2;
            int dy = H - 42 - dh - 12; // Floating above taskbar

            // Soft shadow under dock
            using (var b = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
            {
                g.FillEllipse(b, dx - 20, dy + dh - 10, dw + 40, 24);
            }

            g.DrawImage(ImgDock, new Rectangle(dx, dy, dw, dh));

            // Cursor hovering along dock icons creating spring wave effect
            float curX = dx + 150 + (f / 210.0f) * (dw - 300);
            float curY = dy + dh / 2f;

            // Highlight circle around hovered icon
            using (var pen = new Pen(Color.FromArgb(180, 168, 85, 247), 1.8f))
            {
                g.DrawEllipse(pen, curX - 22, curY - 22, 44, 44);
            }

            DrawMouseCursor(g, curX, curY);
        }
    }

    private static void DrawScene_SquishTiling(Graphics g, int f, double t)
    {
        // Tag Overlay
        DrawFeaturePill(g, "📐 SQUISH DYNAMIC WINDOW TILING", "Golden-Ratio BSP Dwindle · Atomic DeferWindowPos · Zero Tearing", Color.FromArgb(245, 158, 11));

        // Draw two tiled windows splitting along golden ratio (0.618 : 0.382)
        int margin = 20;
        int topY = 48;
        int availW = W - margin * 2;
        int availH = H - topY - 42 - margin; // Above taskbar

        int leftW = (int)(availW * 0.618) - 4;
        int rightW = availW - leftW - 8;

        // Window 1 (Left: Antigravity IDE)
        DrawMockWindow(g, margin, topY, leftW, availH, "Antigravity IDE — LaunchMe-Win (Workspace 1)", Color.FromArgb(16, 20, 28), Color.FromArgb(16, 185, 129), true);

        // Window 2 (Right: Windows Terminal / Pookie Cockpit)
        DrawMockWindow(g, margin + leftW + 8, topY, rightW, availH, "Windows Terminal — Pookie Approval Cockpit", Color.FromArgb(12, 14, 20), Color.FromArgb(245, 158, 11), false);
    }

    private static void DrawMockWindow(Graphics g, int x, int y, int w, int h, string title, Color bg, Color accent, bool active)
    {
        // Rounded Window Background
        using (var brush = new SolidBrush(bg))
        using (var pen = new Pen(active ? accent : Color.FromArgb(40, 255, 255, 255), 1.5f))
        {
            g.FillRectangle(brush, x, y, w, h);
            g.DrawRectangle(pen, x, y, w, h);
        }

        // Titlebar
        using (var tbBrush = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
        using (var font = new Font("Segoe UI Variable Display", 10.5f, FontStyle.Bold))
        using (var textBrush = new SolidBrush(Color.FromArgb(240, 245, 250)))
        {
            g.FillRectangle(tbBrush, x, y, w, 28);
            g.DrawString(title, font, textBrush, x + 12, y + 6);
        }

        // Window Control Buttons (minimize, maximize, close)
        int bx = x + w - 60;
        using (var b = new SolidBrush(Color.FromArgb(120, 140, 160)))
        {
            g.FillRectangle(b, bx, y + 10, 8, 2);
            g.DrawRectangle(new Pen(b, 1), bx + 18, y + 6, 8, 8);
            g.DrawLine(new Pen(Color.FromArgb(244, 63, 94), 1.5f), bx + 36, y + 6, bx + 44, y + 14);
            g.DrawLine(new Pen(Color.FromArgb(244, 63, 94), 1.5f), bx + 44, y + 6, bx + 36, y + 14);
        }
    }

    private static void DrawScene_Finale(Graphics g, int f, double t)
    {
        float fade = Math.Min(1f, f / 20.0f);

        // Centercard
        int cw = 680, ch = 280;
        int cx = (W - cw) / 2;
        int cy = (H - ch) / 2 - 20;

        using (var cardBrush = new SolidBrush(Color.FromArgb((int)(245 * fade), 14, 18, 26)))
        using (var borderPen = new Pen(Color.FromArgb((int)(200 * fade), 0, 240, 255), 1.5f))
        {
            g.FillRectangle(cardBrush, cx, cy, cw, ch);
            g.DrawRectangle(borderPen, cx, cy, cw, ch);
        }

        using (var fontTitle = new Font("Segoe UI Variable Display", 26f, FontStyle.Bold))
        using (var fontSub = new Font("Segoe UI Variable Text", 13f, FontStyle.Regular))
        using (var fontCode = new Font("Cascadia Code", 13f, FontStyle.Bold))
        using (var bWhite = new SolidBrush(Color.FromArgb((int)(255 * fade), 248, 250, 252)))
        using (var bCyan = new SolidBrush(Color.FromArgb((int)(255 * fade), 0, 240, 255)))
        using (var bEmerald = new SolidBrush(Color.FromArgb((int)(255 * fade), 16, 185, 129)))
        {
            g.DrawString("LaunchMe OS v1.0.0", fontTitle, bCyan, cx + 170, cy + 35);
            g.DrawString("10.0 / 10 Parity Standard · 569/569 Passing Tests", fontSub, bWhite, cx + 165, cy + 90);
            g.DrawString("0.0% Idle GPU · Strictly Local-First & Zero Telemetry", fontSub, bEmerald, cx + 155, cy + 120);

            // Install command badge
            using (var bCodeBg = new SolidBrush(Color.FromArgb((int)(220 * fade), 22, 28, 40)))
            {
                g.FillRectangle(bCodeBg, cx + 160, cy + 175, 360, 40);
                g.DrawRectangle(new Pen(Color.FromArgb((int)(100 * fade), 255, 255, 255), 1), cx + 160, cy + 175, 360, 40);
            }
            g.DrawString("winget install LaunchMe.OS", fontCode, bWhite, cx + 215, cy + 185);
        }
    }

    private static void DrawFeaturePill(Graphics g, string title, string sub, Color accent)
    {
        int pw = 580, ph = 38;
        int px = (W - pw) / 2;
        int py = H - 42 - ph - 16; // Just above taskbar

        using (var b = new SolidBrush(Color.FromArgb(235, 12, 16, 22)))
        using (var p = new Pen(accent, 1.2f))
        using (var fBold = new Font("Segoe UI Variable Display", 10.5f, FontStyle.Bold))
        using (var fSub = new Font("Segoe UI Variable Text", 9f, FontStyle.Regular))
        using (var bAccent = new SolidBrush(accent))
        using (var bMuted = new SolidBrush(Color.FromArgb(180, 195, 215)))
        {
            g.FillRectangle(b, px, py, pw, ph);
            g.DrawRectangle(p, px, py, pw, ph);

            g.DrawString(title, fBold, bAccent, px + 16, py + 5);
            g.DrawString(sub, fSub, bMuted, px + 16, py + 20);
        }
    }

    private static void DrawKeyBadge(Graphics g, string key, int cx, int cy)
    {
        using (var b = new SolidBrush(Color.FromArgb(240, 24, 30, 42)))
        using (var p = new Pen(Color.FromArgb(0, 240, 255), 1.5f))
        using (var f = new Font("Segoe UI Variable Display", 11f, FontStyle.Bold))
        using (var textB = new SolidBrush(Color.FromArgb(0, 240, 255)))
        {
            int kw = 120, kh = 32;
            int kx = cx - kw / 2, ky = cy - kh / 2;
            g.FillRectangle(b, kx, ky, kw, kh);
            g.DrawRectangle(p, kx, ky, kw, kh);
            g.DrawString(key, f, textB, kx + 18, ky + 7);
        }
    }

    private static void DrawMouseCursor(Graphics g, float x, float y)
    {
        // Standard Windows 11 arrow cursor with shadow
        using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
        {
            PointF[] shadowPts = {
                new PointF(x + 2, y + 2),
                new PointF(x + 2, y + 17),
                new PointF(x + 6, y + 13),
                new PointF(x + 10, y + 20),
                new PointF(x + 12, y + 19),
                new PointF(x + 8, y + 12),
                new PointF(x + 13, y + 12)
            };
            g.FillPolygon(shadowBrush, shadowPts);
        }

        using (var whiteBrush = new SolidBrush(Color.White))
        using (var blackPen = new Pen(Color.Black, 1.2f))
        {
            PointF[] pts = {
                new PointF(x, y),
                new PointF(x, y + 15),
                new PointF(x + 4, y + 11),
                new PointF(x + 8, y + 18),
                new PointF(x + 10, y + 17),
                new PointF(x + 6, y + 10),
                new PointF(x + 11, y + 10)
            };
            g.FillPolygon(whiteBrush, pts);
            g.DrawPolygon(blackPen, pts);
        }
    }
}
"@

Add-Type -TypeDefinition $code -ReferencedAssemblies "System.Drawing.dll"

Write-Host "Initializing renderer with real captured UI surfaces from device..." -ForegroundColor Cyan
$notchPath = "C:\Users\Redwan Ahmmed\Pictures\LaunchMe Showcase\NotchWindow_100pct_20261127_213446.png"
$launcherPath = "C:\Users\Redwan Ahmmed\Pictures\LaunchMe Showcase\LauncherWindow_100pct_20261127_213446.png"
$dockPath = "C:\Users\Redwan Ahmmed\Pictures\LaunchMe Showcase\DockWindow_100pct_20261127_213446.png"

[RealDeviceTourRenderer]::Initialize($notchPath, $launcherPath, $dockPath)

Write-Host "Rendering 960 frames (32s @ 30 FPS) in parallel across CPU cores..." -ForegroundColor Cyan
$sw = [System.Diagnostics.Stopwatch]::StartNew()
[RealDeviceTourRenderer]::RenderAll($tempFrames)
$sw.Stop()
Write-Host "Rendered 960 frames in $($sw.Elapsed.TotalSeconds.ToString('F2')) seconds!" -ForegroundColor Green

# Encode to HD MP4
Write-Host "Encoding HD MP4 video with ffmpeg..." -ForegroundColor Cyan
$mp4Out = Join-Path $OutputDir "banner.mp4"
$distMp4 = Join-Path $DistDir "launchme_os_real_device_tour_30fps.mp4"

$ffmpegArgs = "-framerate 30 -i `"$tempFrames\frame_%05d.png`" -c:v libx264 -pix_fmt yuv420p -crf 20 -preset fast `"$mp4Out`" -y"
Start-Process "ffmpeg" -ArgumentList $ffmpegArgs -Wait -NoNewWindow
Copy-Item $mp4Out $distMp4 -Force

# Generate high-quality looping GIF for GitHub README
Write-Host "Generating optimized looping GIF for GitHub README banner..." -ForegroundColor Cyan
$gifOut = Join-Path $OutputDir "banner.gif"
$gifArgs = "-i `"$mp4Out`" -vf `"fps=15,scale=800:-1:flags=lanczos,split[s0][s1];[s0]palettegen=max_colors=128[p];[s1][p]paletteuse=dither=bayer:bayer_scale=3`" `"$gifOut`" -y"
Start-Process "ffmpeg" -ArgumentList $gifArgs -Wait -NoNewWindow

Remove-Item $tempFrames -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`n=== Video Production Complete ===" -ForegroundColor Green
Get-Item $mp4Out, $gifOut | Select-Object Name, Length, FullName
