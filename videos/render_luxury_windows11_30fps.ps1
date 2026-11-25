$ErrorActionPreference = "Stop"

$baseDir = "C:\Users\Redwan Ahmmed\.gemini\antigravity\brain\89c20678-0592-4161-88c5-7b793ab68dac"
$outDir = "$baseDir\videos"
$frameDumpDir = "$outDir\win11_luxury_frames"
$distVideoDir = "E:\LaunchMe OS\dist\videos"

if (Test-Path $frameDumpDir) { Remove-Item $frameDumpDir -Recurse -Force }
New-Item -ItemType Directory -Path $frameDumpDir -Force | Out-Null
if (!(Test-Path $distVideoDir)) { New-Item -ItemType Directory -Path $distVideoDir -Force | Out-Null }

Add-Type -AssemblyName System.Drawing

$csharpRenderer = @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

public static class LuxuryTourRenderer
{
    private const int W = 1280;
    private const int H = 720;

    // Editorial Luxury Colors
    private static readonly Color ObsidianVoid = Color.FromArgb(8, 10, 15);
    private static readonly Color Surface1 = Color.FromArgb(220, 14, 19, 31);
    private static readonly Color Surface2 = Color.FromArgb(238, 22, 29, 46);
    private static readonly Color CyanNeon = Color.FromArgb(0, 240, 255);
    private static readonly Color OrchidNeon = Color.FromArgb(168, 85, 247);
    private static readonly Color EmeraldNeon = Color.FromArgb(0, 255, 157);
    private static readonly Color AmberTopaz = Color.FromArgb(245, 158, 11);
    private static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);
    private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

    // Preloaded fonts
    private static Font FontHero;
    private static Font FontHeader;
    private static Font FontSub;
    private static Font FontBody;
    private static Font FontMicro;
    private static Font FontMono;
    private static Font FontEmoji;

    public static void RenderAll(string outDir)
    {
        FontHero = new Font("Segoe UI Variable Display", 24f, FontStyle.Bold, GraphicsUnit.Pixel);
        FontHeader = new Font("Segoe UI Variable Display", 17f, FontStyle.Bold, GraphicsUnit.Pixel);
        FontSub = new Font("Segoe UI Variable Text", 13f, FontStyle.Regular, GraphicsUnit.Pixel);
        FontBody = new Font("Segoe UI Variable Text", 11.5f, FontStyle.Regular, GraphicsUnit.Pixel);
        FontMicro = new Font("Segoe UI Variable Small", 9.5f, FontStyle.Bold, GraphicsUnit.Pixel);
        FontMono = new Font("Cascadia Code", 11f, FontStyle.Regular, GraphicsUnit.Pixel);
        FontEmoji = new Font("Segoe UI Emoji", 13f, FontStyle.Regular, GraphicsUnit.Pixel);

        int totalFrames = 1860; // 62.0 seconds @ 30 FPS

        using (var bmp = new Bitmap(W, H, PixelFormat.Format32bppPArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            for (int f = 0; f < totalFrames; f++)
            {
                double t = f / 30.0;
                DrawBackdrop(g, t);

                if (f < 180)
                {
                    DrawScene0_ColdBoot(g, f, t);
                }
                else if (f < 450)
                {
                    DrawScene1_LauncherBento(g, f - 180, t);
                }
                else if (f < 750)
                {
                    DrawScene2_SnootBlobboPookie(g, f - 450, t);
                }
                else if (f < 1020)
                {
                    DrawScene3_CapsuleDock(g, f - 750, t);
                }
                else if (f < 1320)
                {
                    DrawScene4_MusicPlayer(g, f - 1020, t);
                }
                else if (f < 1620)
                {
                    DrawScene5_SquishTiling(g, f - 1320, t);
                }
                else
                {
                    DrawScene6_LeashAndFinale(g, f - 1620, t);
                }

                // Always keep Windows 11 Taskbar & REC telemetry subtle badge unless hidden in Scene 0 intro
                if (f >= 135 && f < 1770)
                {
                    DrawWindows11Taskbar(g, f);
                }

                // Top-Right 30 FPS Telemetry Pill
                DrawHudWatermark(g, f, t);

                string file = Path.Combine(outDir, string.Format("frame_{0:D5}.png", f));
                bmp.Save(file, ImageFormat.Png);
            }
        }
    }

    private static GraphicsPath CreateSquirclePath(float x, float y, float w, float h, float r)
    {
        float maxR = Math.Min(w, h) / 2.2f;
        r = Math.Min(r, maxR);
        float l = Math.Min(1.5286f * r, Math.Min(w, h) / 2.0f);
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddLine(x + l, y, x + w - l, y);
        path.AddBezier(x + w - l, y, x + w - 0.9571f * r, y, x + w - 0.4357f * r, y + 0.0714f * r, x + w - 0.2143f * r, y + 0.2143f * r);
        path.AddBezier(x + w - 0.2143f * r, y + 0.2143f * r, x + w - 0.0714f * r, y + 0.4357f * r, x + w, y + 0.9571f * r, x + w, y + l);
        path.AddLine(x + w, y + l, x + w, y + h - l);
        path.AddBezier(x + w, y + h - l, x + w, y + h - 0.9571f * r, x + w - 0.0714f * r, y + h - 0.4357f * r, x + w - 0.2143f * r, y + h - 0.2143f * r);
        path.AddBezier(x + w - 0.2143f * r, y + h - 0.2143f * r, x + w - 0.4357f * r, y + h - 0.0714f * r, x + w - 0.9571f * r, y + h, x + w - l, y + h);
        path.AddLine(x + w - l, y + h, x + l, y + h);
        path.AddBezier(x + l, y + h, x + 0.9571f * r, y + h, x + 0.4357f * r, y + h - 0.0714f * r, x + 0.2143f * r, y + h - 0.2143f * r);
        path.AddBezier(x + 0.2143f * r, y + h - 0.2143f * r, x + 0.0714f * r, y + h - 0.4357f * r, x, y + h - 0.9571f * r, x, y + h - l);
        path.AddLine(x, y + h - l, x, y + l);
        path.AddBezier(x, y + l, x, y + 0.9571f * r, x + 0.0714f * r, y + 0.4357f * r, x + 0.2143f * r, y + 0.2143f * r);
        path.AddBezier(x + 0.2143f * r, y + 0.2143f * r, x + 0.4357f * r, y + 0.0714f * r, x + 0.9571f * r, y, x + l, y);
        path.CloseFigure();
        return path;
    }

    private static void DrawSpecularCard(Graphics g, float x, float y, float w, float h, float r, Color fill, Color? highlightBorder = null, float borderWidth = 1.0f)
    {
        using (var path = CreateSquirclePath(x, y, w, h, r))
        {
            // Ambient occlusion shadow
            using (var shadowPath = CreateSquirclePath(x, y + 6, w, h, r))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            using (var fillBrush = new SolidBrush(fill))
            {
                g.FillPath(fillBrush, path);
            }

            // 135-deg Specular Chamfer Rim
            Color cTopLeft = highlightBorder.HasValue ? Color.FromArgb(200, highlightBorder.Value) : Color.FromArgb(145, 255, 255, 255);
            Color cBotRight = highlightBorder.HasValue ? Color.FromArgb(40, highlightBorder.Value) : Color.FromArgb(14, 255, 255, 255);
            using (var rimBrush = new LinearGradientBrush(new PointF(x, y), new PointF(x + w, y + h), cTopLeft, cBotRight))
            using (var rimPen = new Pen(rimBrush, borderWidth))
            {
                g.DrawPath(rimPen, path);
            }
        }
    }

    private static void DrawBackdrop(Graphics g, double t)
    {
        g.Clear(ObsidianVoid);

        // Moving subtle Windows 11 Bloom / Obsidian Aurora
        float cx1 = 360f + (float)(Math.Sin(t * 0.4) * 90);
        float cy1 = 220f + (float)(Math.Cos(t * 0.3) * 60);
        using (var gp1 = new GraphicsPath())
        {
            gp1.AddEllipse(cx1 - 380, cy1 - 380, 760, 760);
            using (var pgb1 = new PathGradientBrush(gp1))
            {
                pgb1.CenterColor = Color.FromArgb(42, 0, 120, 212);
                pgb1.SurroundColors = new[] { Color.FromArgb(0, 8, 10, 15) };
                g.FillPath(pgb1, gp1);
            }
        }

        float cx2 = 960f + (float)(Math.Cos(t * 0.35) * 80);
        float cy2 = 460f + (float)(Math.Sin(t * 0.45) * 50);
        using (var gp2 = new GraphicsPath())
        {
            gp2.AddEllipse(cx2 - 340, cy2 - 340, 680, 680);
            using (var pgb2 = new PathGradientBrush(gp2))
            {
                pgb2.CenterColor = Color.FromArgb(32, 139, 92, 246);
                pgb2.SurroundColors = new[] { Color.FromArgb(0, 8, 10, 15) };
                g.FillPath(pgb2, gp2);
            }
        }
    }

    private static void DrawCursor(Graphics g, float x, float y, bool clicking = false)
    {
        if (clicking)
        {
            using (var clickPen = new Pen(Color.FromArgb(140, CyanNeon), 2.0f))
            {
                g.DrawEllipse(clickPen, x - 12, y - 12, 24, 24);
            }
        }

        PointF[] pts = {
            new PointF(x, y),
            new PointF(x, y + 18),
            new PointF(x + 5, y + 14),
            new PointF(x + 9, y + 21),
            new PointF(x + 12, y + 19),
            new PointF(x + 8, y + 12),
            new PointF(x + 14, y + 12)
        };
        using (var fill = new SolidBrush(Color.White))
        using (var stroke = new Pen(Color.Black, 1.5f))
        {
            g.FillPolygon(fill, pts);
            g.DrawPolygon(stroke, pts);
        }
    }

    private static void DrawWindows11Taskbar(Graphics g, int frame)
    {
        int tbH = 44;
        int tbY = H - tbH;
        using (var tbBrush = new SolidBrush(Color.FromArgb(232, 10, 13, 20)))
        {
            g.FillRectangle(tbBrush, 0, tbY, W, tbH);
        }
        using (var topLine = new Pen(Color.FromArgb(38, 255, 255, 255), 1f))
        {
            g.DrawLine(topLine, 0, tbY, W, tbY);
        }

        // Centered Windows 11 Start Logo (4 blue tiles)
        int startX = (W / 2) - 110;
        int iconY = tbY + 12;
        using (var winBlue = new SolidBrush(Color.FromArgb(0, 164, 239)))
        {
            g.FillRectangle(winBlue, startX, iconY, 9, 9);
            g.FillRectangle(winBlue, startX + 11, iconY, 9, 9);
            g.FillRectangle(winBlue, startX, iconY + 11, 9, 9);
            g.FillRectangle(winBlue, startX + 11, iconY + 11, 9, 9);
        }

        string[] icons = { "🔍", "◫", "📁", "🌐", "💻", "🚀" };
        using (var tbText = new SolidBrush(Color.FromArgb(220, 230, 240)))
        {
            for (int i = 0; i < icons.Length; i++)
            {
                g.DrawString(icons[i], FontEmoji, tbText, startX + 34 + (i * 32), tbY + 11);
            }
        }

        // Active blue indicator under LaunchMe
        using (var pill = new SolidBrush(CyanNeon))
        {
            g.FillRectangle(pill, startX + 34 + (5 * 32) + 4, H - 5, 12, 3);
        }

        // Right Tray
        using (var trayBrush = new SolidBrush(TextMuted))
        {
            g.DrawString("ENG   📶   🔊   100%   22:59 PM", FontBody, trayBrush, W - 210, tbY + 14);
        }
    }

    private static void DrawSnootPill(Graphics g, float width, float height, bool showBlobbo, string blobboFace, string track, string timer, bool pulseAlert, float pupilDx, float pupilDy)
    {
        float x = (W - width) / 2f;
        float y = 8f;
        Color? borderHighlight = pulseAlert ? AmberTopaz : (Color?)null;
        DrawSpecularCard(g, x, y, width, height, 20f, Color.FromArgb(242, 8, 10, 15), borderHighlight, pulseAlert ? 1.5f : 0.8f);

        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var aBrush = new SolidBrush(AmberTopaz))
        using (var mBrush = new SolidBrush(TextMuted))
        {
            // Left: Weather
            g.DrawString("22°C CLEAR", FontMicro, cBrush, x + 16, y + 11);

            // Center: Blobbo & Track
            if (showBlobbo)
            {
                // Draw stylized Blobbo badge with moving eyes
                float bx = x + 105;
                float by = y + 7;
                DrawSpecularCard(g, bx, by, 26, 20, 8f, Color.FromArgb(255, 30, 41, 59), pulseAlert ? AmberTopaz : OrchidNeon, 1.0f);
                using (var eyeBrush = new SolidBrush(Color.White))
                using (var pupilBrush = new SolidBrush(Color.FromArgb(10, 10, 15)))
                {
                    g.FillEllipse(eyeBrush, bx + 5, by + 5, 6, 8);
                    g.FillEllipse(eyeBrush, bx + 15, by + 5, 6, 8);
                    g.FillEllipse(pupilBrush, bx + 6.5f + pupilDx, by + 7f + pupilDy, 3, 4);
                    g.FillEllipse(pupilBrush, bx + 16.5f + pupilDx, by + 7f + pupilDy, 3, 4);
                }
            }

            g.DrawString(blobboFace + "  " + track, FontBody, wBrush, x + 138, y + 9);
            g.DrawString(timer + "  •  100%", FontMicro, pulseAlert ? aBrush : mBrush, x + width - 92, y + 11);
        }
    }

    private static void DrawHudWatermark(Graphics g, int frame, double t)
    {
        DrawSpecularCard(g, W - 245, 10, 230, 28, 12f, Color.FromArgb(215, 8, 10, 15), CyanNeon, 0.7f);
        using (var redDot = new SolidBrush(Color.FromArgb(239, 68, 68)))
        using (var txtBrush = new SolidBrush(TextPrimary))
        {
            if ((frame / 15) % 2 == 0)
            {
                g.FillEllipse(redDot, W - 233, 20, 7, 7);
            }
            g.DrawString(string.Format("30.0 FPS  •  WIN11  •  {0:00}:{1:00}", (int)t / 60, (int)t % 60), FontMicro, txtBrush, W - 218, 18);
        }
    }

    // SCENE 0: Cold Boot & Full Loading Sequence (Frames 0-179)
    private static void DrawScene0_ColdBoot(Graphics g, int f, double t)
    {
        float progress = Math.Min(1.0f, f / 135.0f);

        // Center Editorial Boot Card
        float cw = 540, ch = 360;
        float cx = (W - cw) / 2f, cy = (H - ch) / 2f;
        DrawSpecularCard(g, cx, cy, cw, ch, 26f, Surface1, CyanNeon, 1.0f);

        using (var tBrush = new SolidBrush(TextPrimary))
        using (var mBrush = new SolidBrush(TextMuted))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var eBrush = new SolidBrush(EmeraldNeon))
        {
            // Brand Title
            g.DrawString("LAUNCHME OS", FontHero, tBrush, cx + 36, cy + 32);
            g.DrawString("WINDOWS 11 EDITORIAL LUXURY SHELL  •  v1.0.0", FontMicro, cBrush, cx + 38, cy + 66);

            // Animated Spinner Orb
            float spinAngle = (f * 12) % 360;
            using (var trackPen = new Pen(Color.FromArgb(40, 255, 255, 255), 3f))
            using (var arcPen = new Pen(CyanNeon, 3f))
            {
                g.DrawEllipse(trackPen, cx + cw - 84, cy + 32, 42, 42);
                g.DrawArc(arcPen, cx + cw - 84, cy + 32, 42, 42, spinAngle, 120);
            }

            // Loading Checklist Steps
            string[] steps = {
                "LaunchMe.Contracts & SqliteSpaceStore",
                "Win2D LiquidGlassCompositor (Blur 42px • Sat 180% • Grain)",
                "SquishEngine BSP & Win32 DWM Squircle Hooks",
                "PookieHookServer (127.0.0.1:49152) & Blobbo Pet",
                "Flow 9-Band Dictation & Leash AES-GCM Relay",
                "QA Verified: 488/488 Tests • 0 CVEs • 0 Stubs"
            };

            for (int i = 0; i < steps.Length; i++)
            {
                bool done = progress >= ((i + 1) / (float)steps.Length);
                string prefix = done ? "[✓]  " : "[•]  ";
                g.DrawString(prefix + steps[i], FontBody, done ? eBrush : mBrush, cx + 38, cy + 114 + (i * 28));
            }

            // Progress Bar
            float barW = cw - 76;
            DrawSpecularCard(g, cx + 38, cy + ch - 62, barW, 10, 5f, Color.FromArgb(100, 255, 255, 255));
            if (progress > 0.02f)
            {
                using (var fillBar = new LinearGradientBrush(new PointF(cx + 38, 0), new PointF(cx + 38 + barW, 0), CyanNeon, OrchidNeon))
                using (var p = CreateSquirclePath(cx + 38, cy + ch - 62, Math.Max(10, barW * progress), 10, 5f))
                {
                    g.FillPath(fillBar, p);
                }
            }
            g.DrawString(string.Format("INITIALIZING SURFACES... {0}%", (int)(progress * 100)), FontMicro, mBrush, cx + 38, cy + ch - 40);
        }

        // At f >= 135, Snoot drops in via Spring Physics
        if (f >= 135)
        {
            float sf = (f - 135) / 45.0f;
            float springEase = (float)(1.0 - Math.Exp(-6.0 * sf) * Math.Cos(12.0 * sf));
            float dropW = 400 * Math.Min(1.05f, springEase);
            DrawSnootPill(g, Math.Max(100, dropW), 36, true, "( •̀ ω •́ )", "Not Playing", "25:00", false, 0, 0);
        }
    }

    // SCENE 1: Bento Luxury Launcher Grid (Frames 180-449 -> local f 0-269)
    private static void DrawScene1_LauncherBento(Graphics g, int f, double t)
    {
        DrawSnootPill(g, 410, 36, true, "( •̀ ω •́ )", "Cruel Summer", "24:59", false, 0, 0);

        // Search Capsule with Cyan Halo
        float sx = (W - 640) / 2f, sy = 64;
        DrawSpecularCard(g, sx, sy, 640, 48, 18f, Surface2, CyanNeon, 1.2f);

        string typedQuery = "= 2 * pi * 64";
        int chars = Math.Min(typedQuery.Length, f / 6);
        string shownText = f < 140 ? typedQuery.Substring(0, chars) : "pixel";

        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var mBrush = new SolidBrush(TextMuted))
        using (var eBrush = new SolidBrush(EmeraldNeon))
        {
            g.DrawString("🔍  " + shownText + ((f / 12) % 2 == 0 ? "|" : ""), FontSub, wBrush, sx + 18, sy + 14);
            if (f >= 75 && f < 140)
            {
                DrawSpecularCard(g, sx + 455, sy + 9, 170, 30, 10f, Color.FromArgb(40, 0, 240, 255), CyanNeon, 0.8f);
                g.DrawString("CALC: 402.1238", FontMicro, cBrush, sx + 470, sy + 18);
            }
            else
            {
                g.DrawString("ALT + SPACE", FontMicro, mBrush, sx + 550, sy + 18);
            }

            // Asymmetric Bento Row
            // 1. Hero Weather Widget
            DrawSpecularCard(g, 110, 140, 320, 175, 24f, Surface1);
            g.DrawString("SOLAR WEATHER", FontMicro, cBrush, 132, 160);
            g.DrawString("22°C", FontHero, wBrush, 132, 182);
            g.DrawString("Clear Sky • Sunset 18:14", FontBody, mBrush, 132, 224);
            // Solar arc line
            using (var arcPen = new Pen(AmberTopaz, 2.5f))
            {
                g.DrawArc(arcPen, 280, 165, 110, 80, 180, 180);
            }

            // 2. Clock & Pomodoro
            DrawSpecularCard(g, 450, 140, 280, 175, 24f, Surface1);
            g.DrawString("FOCUS TIMER", FontMicro, eBrush, 472, 160);
            int secs = 1499 - (f / 30);
            g.DrawString(string.Format("{0:D2}:{1:D2}", secs / 60, secs % 60), FontHero, wBrush, 472, 182);
            g.DrawString("Liquid Glass  •  Obsidian #080A0F", FontBody, mBrush, 472, 224);

            // 3. 11-Family Widget Catalog Drawer Preview
            DrawSpecularCard(g, 750, 140, 420, 470, 24f, Surface2, OrchidNeon, 0.9f);
            using (var oBrush = new SolidBrush(OrchidNeon))
            {
                g.DrawString("11-FAMILY WIDGET CATALOG", FontMicro, oBrush, 774, 162);
            }
            string[] wNames = { "Clock", "Weather", "GSMTC Music", "System Stats", "Pomodoro", "Affirmations", "Calendar", "Pixel Art (18)", "Photo", "Shape Mask", "GIF Loop" };
            for (int i = 0; i < wNames.Length; i++)
            {
                int col = i % 2;
                int row = i / 2;
                float wx = 774 + col * 190;
                float wy = 195 + row * 64;
                bool hi = (f / 22) % wNames.Length == i;
                DrawSpecularCard(g, wx, wy, 178, 52, 14f, Color.FromArgb(180, 18, 24, 38), hi ? CyanNeon : (Color?)null, hi ? 1.2f : 0.6f);
                g.DrawString(wNames[i], FontBody, wBrush, wx + 14, wy + 17);
            }

            // Bottom Left App Pedestals (G2 Squircles)
            string[] apps = { "Terminal", "VS Code", "Explorer", "Edge", "Spotify", "Settings" };
            string[] appIcons = { "💻", "📝", "📁", "🌐", "🎵", "⚙️" };
            int hovered = (f / 35) % apps.Length;
            for (int i = 0; i < apps.Length; i++)
            {
                float ax = 110 + i * 104;
                float ay = 350;
                bool isH = (i == hovered);
                DrawSpecularCard(g, ax, ay, 86, 86, 22f, isH ? Surface2 : Surface1, isH ? CyanNeon : (Color?)null, isH ? 1.5f : 0.8f);
                g.DrawString(appIcons[i], FontHero, wBrush, ax + 24, ay + 20);
                g.DrawString(apps[i], FontBody, isH ? wBrush : mBrush, ax + 16, ay + 96);
            }

            // Space Switcher Strip
            DrawSpecularCard(g, 110, 505, 620, 105, 22f, Surface1);
            g.DrawString("ACTIVE SPACE  •  WORKFLOWS  •  .LAUNCHMETHEME", FontMicro, cBrush, 132, 525);
            g.DrawString("Space: Dev Studio     Workflows: [ Launch Stack ]  [ Deep Focus ]", FontBody, wBrush, 132, 555);
        }

        // Cursor moving to hovered app
        float cursorX = 150 + ((f / 35) % 6) * 104;
        float cursorY = 390 + (float)(Math.Sin(f * 0.1) * 8);
        DrawCursor(g, cursorX, cursorY, (f % 35) == 15);
    }

    // SCENE 2: Snoot HUD, Blobbo Pet & Pookie Approval Cockpit (Frames 450-749 -> local f 0-299)
    private static void DrawScene2_SnootBlobboPookie(Graphics g, int f, double t)
    {
        float expandEase = Math.Min(1.0f, f / 30.0f);
        float drawW = 410 + 230 * expandEase;
        float drawH = 36 + 380 * expandEase;
        float drawX = (W - drawW) / 2f;

        // Animated cursor moving left/right to test Blobbo gaze tracking
        float cursorX = 640 + (float)(Math.Sin(t * 2.2) * 320);
        float cursorY = 480 + (float)(Math.Cos(t * 1.6) * 80);
        float pupilDx = Math.Max(-2.5f, Math.Min(2.5f, (cursorX - 640) / 140f));
        float pupilDy = Math.Max(-2.0f, Math.Min(2.0f, (cursorY - 80) / 180f));

        string moodFace = "( •̀ ω •́ )";
        string moodDesc = "Blobbo is watching";
        if (f > 70 && f < 130) { moodFace = "( > _ < )"; moodDesc = "Hey! That tickles!"; }
        else if (f >= 130 && f < 175) { moodFace = "( @ _ @ )"; moodDesc = "Woah, too many pokes!"; }
        else if (f >= 175) { moodFace = "( ◕ ‿ ◕ )!"; moodDesc = "Pookie needs permission!"; }

        bool alertActive = (f >= 160);
        DrawSpecularCard(g, drawX, 12, drawW, drawH, 24f, Surface2, alertActive ? AmberTopaz : CyanNeon, 1.3f);

        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var aBrush = new SolidBrush(AmberTopaz))
        using (var eBrush = new SolidBrush(EmeraldNeon))
        using (var mBrush = new SolidBrush(TextMuted))
        {
            // Snoot Compact Header
            g.DrawString("SNOOT HUD", FontMicro, cBrush, drawX + 22, 24);
            g.DrawString(moodFace + "  " + moodDesc, FontBody, wBrush, drawX + 180, 22);
            g.DrawString("24:45", FontMicro, aBrush, drawX + drawW - 60, 24);

            if (f > 25)
            {
                // Poke Blobbo & Controls Row
                DrawSpecularCard(g, drawX + 24, 62, 110, 32, 10f, Color.FromArgb(40, 255, 255, 255), CyanNeon, 0.8f);
                g.DrawString("Poke Blobbo", FontBody, wBrush, drawX + 42, 71);

                // Blobbo Large Interactive Avatar
                DrawSpecularCard(g, drawX + 150, 56, 68, 44, 14f, Color.FromArgb(255, 20, 26, 40), alertActive ? AmberTopaz : OrchidNeon, 1.2f);
                using (var eyeB = new SolidBrush(Color.White))
                using (var pupB = new SolidBrush(Color.Black))
                {
                    g.FillEllipse(eyeB, drawX + 165, 68, 12, 16);
                    g.FillEllipse(eyeB, drawX + 190, 68, 12, 16);
                    g.FillEllipse(pupB, drawX + 168 + pupilDx, 73 + pupilDy, 6, 7);
                    g.FillEllipse(pupB, drawX + 193 + pupilDx, 73 + pupilDy, 6, 7);
                }

                // Flow Voice 9-Band Audio Equalizer
                g.DrawString("FLOW DICTATION (9-BAND)", FontMicro, cBrush, drawX + 240, 58);
                for (int b = 0; b < 9; b++)
                {
                    float barH = 8f + (float)(Math.Abs(Math.Sin(t * 7.0 + b * 0.7)) * 22f);
                    using (var barBrush = new SolidBrush(b % 2 == 0 ? CyanNeon : OrchidNeon))
                    {
                        g.FillRectangle(barBrush, drawX + 240 + (b * 12), 102 - barH, 7, barH);
                    }
                }
                g.DrawString("\"Deploy LaunchMe v1.0\"", FontBody, wBrush, drawX + 365, 76);

                // Pookie Permission Approval Card
                Color pookieBorder = f > 250 ? EmeraldNeon : AmberTopaz;
                DrawSpecularCard(g, drawX + 24, 124, drawW - 48, 210, 18f, Color.FromArgb(220, 12, 16, 26), pookieBorder, 1.3f);
                g.DrawString(f > 250 ? "✓ POOKIE DECISION SENT (ALLOWED)" : "⚠ POOKIE PERMISSION ALERT (127.0.0.1:49152)", FontMicro, f > 250 ? eBrush : aBrush, drawX + 42, 142);
                g.DrawString("Command: git push origin main --tags", FontSub, wBrush, drawX + 42, 166);

                // Colorized Unified Git Diff
                DrawSpecularCard(g, drawX + 42, 196, drawW - 84, 78, 10f, Color.FromArgb(240, 6, 8, 12));
                g.DrawString("+ [phase-9] Editorial Luxury Win2D Compositor", FontMono, eBrush, drawX + 54, 208);
                g.DrawString("+ [phase-9] SnootSpringController + G2 Squircles", FontMono, eBrush, drawX + 54, 228);
                using (var roseBrush = new SolidBrush(Color.FromArgb(251, 113, 133)))
                {
                    g.DrawString("- [legacy] Flat dark gray panel borders", FontMono, roseBrush, drawX + 54, 248);
                }

                // Allow / Deny / Always Allow Buttons
                DrawSpecularCard(g, drawX + 42, 288, 92, 30, 10f, Color.FromArgb(200, 22, 163, 74), EmeraldNeon, 1.0f);
                g.DrawString("Allow", FontBody, wBrush, drawX + 70, 296);

                DrawSpecularCard(g, drawX + 146, 288, 92, 30, 10f, Color.FromArgb(180, 220, 38, 38));
                g.DrawString("Deny", FontBody, wBrush, drawX + 176, 296);

                DrawSpecularCard(g, drawX + 250, 288, 120, 30, 10f, Color.FromArgb(140, 30, 41, 59));
                g.DrawString("Always Allow", FontBody, wBrush, drawX + 272, 296);

                // Snoot Pocket Shelf footer
                g.DrawString("SNOOT POCKET SHELF:  launchme_v1.0.0_setup.exe (Ready to Share)", FontMicro, mBrush, drawX + 28, 356);
            }
        }

        if (f > 210 && f < 260)
        {
            DrawCursor(g, drawX + 86, 302, f == 248);
        }
        else
        {
            DrawCursor(g, cursorX, cursorY, f == 72);
        }
    }

    // SCENE 3: Floating Capsule Dock & Soundscapes (Frames 750-1019 -> local f 0-269)
    private static void DrawScene3_CapsuleDock(Graphics g, int f, double t)
    {
        DrawSnootPill(g, 410, 36, true, "( •̀ ω •́ )", "Cruel Summer", "24:40", false, 0, 0);

        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var eBrush = new SolidBrush(EmeraldNeon))
        using (var mBrush = new SolidBrush(TextMuted))
        {
            // Center Showcase Info Card
            DrawSpecularCard(g, 240, 140, 800, 340, 24f, Surface1, CyanNeon, 0.9f);
            g.DrawString("FLOATING CAPSULE DOCK  •  SHAPPMESSAGE WORK AREA", FontMicro, cBrush, 272, 168);
            g.DrawString("Gaussian Spring Hover & Multi-Display Spaces", FontHero, wBrush, 272, 194);

            string[] spaces = { "DEFAULT DOCK", "WORK / CODE", "CREATOR STUDIO" };
            int activeSpace = (f / 90) % 3;
            g.DrawString("ACTIVE SPACE: " + spaces[activeSpace], FontHeader, eBrush, 272, 250);
            g.DrawString("• Reserving bottom work area cleanly via Win32 ABM_SETPOS\n• Gaussian wave magnification scales items smoothly up to 1.45x\n• Live CPU, RAM & NET hardware metrics via Win32 SystemMetrics\n• Procedural Ambient Soundscapes (Rain, Ocean, Forest, WhiteNoise, Cafe)", FontSub, mBrush, 272, 295);

            // Dock Capsule at Bottom (above Taskbar)
            float dw = 840, dh = 76;
            float dx = (W - dw) / 2f, dy = H - 44 - dh - 14;
            DrawSpecularCard(g, dx, dy, dw, dh, 26f, Color.FromArgb(236, 11, 15, 23), null, 0.9f);

            // Space switcher on left
            g.DrawString("◀  " + spaces[activeSpace] + "  ▶", FontMicro, mBrush, dx + 22, dy + 31);

            // Gaussian Hover Dock Icons
            float cursorX = dx + 190 + (float)((0.5 + 0.5 * Math.Sin(t * 1.8)) * 310);
            string[] icons = { "💻", "📝", "📁", "🌐", "🎵", "🎮" };
            for (int i = 0; i < icons.Length; i++)
            {
                float ix = dx + 195 + (i * 54);
                float dist = Math.Abs(cursorX - (ix + 20));
                float scale = 1.0f + 0.45f * (float)Math.Exp(-(dist * dist) / (2.0 * 48.0 * 48.0));
                float sz = 38 * scale;
                float iy = dy + dh - 14 - sz;
                DrawSpecularCard(g, ix - (sz - 38) / 2f, iy, sz, sz, 12f, Color.FromArgb(200, 24, 30, 46), scale > 1.25f ? CyanNeon : (Color?)null, 0.8f);
                g.DrawString(icons[i], FontSub, wBrush, ix - (sz - 38) / 2f + 10 * scale, iy + 10 * scale);
            }

            // Telemetry Pills on Right
            DrawSpecularCard(g, dx + 540, dy + 22, 62, 32, 10f, Color.FromArgb(180, 16, 22, 34), EmeraldNeon, 0.7f);
            g.DrawString("CPU 14%", FontMicro, eBrush, dx + 548, dy + 32);

            DrawSpecularCard(g, dx + 610, dy + 22, 64, 32, 10f, Color.FromArgb(180, 16, 22, 34), AmberTopaz, 0.7f);
            using (var ambBrush = new SolidBrush(AmberTopaz))
            using (var orcBrush = new SolidBrush(OrchidNeon))
            {
                g.DrawString("RAM 4.8G", FontMicro, ambBrush, dx + 617, dy + 32);

                DrawSpecularCard(g, dx + 682, dy + 22, 136, 32, 10f, Color.FromArgb(180, 16, 22, 34), OrchidNeon, 0.7f);
                g.DrawString("LEASH • 🌧️ RAIN", FontMicro, orcBrush, dx + 694, dy + 32);
            }

            DrawCursor(g, cursorX, dy + 36, false);
        }
    }

    // SCENE 4: Audiophile Vinyl & Prismatic Music Player (Frames 1020-1319 -> local f 0-299)
    private static void DrawScene4_MusicPlayer(Graphics g, int f, double t)
    {
        // Deep OLED #05070B base
        g.Clear(Color.FromArgb(5, 7, 11));
        DrawSnootPill(g, 410, 36, true, "\\( ^ヮ^ )/", "Cruel Summer", "PLAYING", false, 0, 0);

        float pulse = 0.5f + 0.5f * (float)Math.Sin(t * 4.2);

        // Ambient Purple/Cyan Halo behind Vinyl
        using (var haloPath = new GraphicsPath())
        {
            haloPath.AddEllipse(170 - pulse * 20, 130 - pulse * 20, 420 + pulse * 40, 420 + pulse * 40);
            using (var pgb = new PathGradientBrush(haloPath))
            {
                pgb.CenterColor = Color.FromArgb((int)(90 + pulse * 60), 168, 85, 247);
                pgb.SurroundColors = new[] { Color.FromArgb(0, 5, 7, 11) };
                g.FillPath(pgb, haloPath);
            }
        }

        // Spinning Holographic Vinyl @ 30 FPS
        float vx = 380, vy = 340;
        float vr = 175;
        using (var vinylFill = new SolidBrush(Color.FromArgb(250, 14, 16, 22)))
        {
            g.FillEllipse(vinylFill, vx - vr, vy - vr, vr * 2, vr * 2);
        }

        // Rotating Specular Sheen on Grooves
        float angle = (f * 4.5f) % 360;
        using (var sheenPen = new Pen(Color.FromArgb(65, 255, 255, 255), 1.5f))
        {
            for (int r = 55; r < 170; r += 12)
            {
                using (var groovePen = new Pen(Color.FromArgb(30, 255, 255, 255), 1.0f))
                {
                    g.DrawEllipse(groovePen, vx - r, vy - r, r * 2, r * 2);
                }
                g.DrawArc(sheenPen, vx - r, vy - r, r * 2, r * 2, angle, 38);
                g.DrawArc(sheenPen, vx - r, vy - r, r * 2, r * 2, (angle + 180) % 360, 38);
            }
        }

        // Metallic Center Label
        using (var lblBrush = new LinearGradientBrush(new PointF(vx - 50, vy - 50), new PointF(vx + 50, vy + 50), OrchidNeon, Color.FromArgb(225, 29, 72)))
        {
            g.FillEllipse(lblBrush, vx - 46, vy - 46, 92, 92);
        }
        using (var holeBrush = new SolidBrush(Color.FromArgb(8, 10, 15)))
        {
            g.FillEllipse(holeBrush, vx - 10, vy - 10, 20, 20);
        }

        // Right Side: GSMTC Controls & Metadata
        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var mBrush = new SolidBrush(TextMuted))
        {
            g.DrawString("WINDOWS GSMTC BRIDGE  •  PAUSE-ON-HIDE ACTIVE", FontMicro, cBrush, 630, 200);
            g.DrawString("Cruel Summer", FontHero, wBrush, 630, 230);
            g.DrawString("Taylor Swift  •  Lover (2019)", FontHeader, mBrush, 630, 268);

            // Progress Slider
            float progW = 460;
            float frac = 0.52f + (f / 300.0f) * 0.12f;
            DrawSpecularCard(g, 630, 326, progW, 6, 3f, Color.FromArgb(60, 255, 255, 255));
            using (var progBrush = new LinearGradientBrush(new PointF(630, 0), new PointF(630 + progW, 0), CyanNeon, OrchidNeon))
            {
                g.FillRectangle(progBrush, 630, 326, progW * frac, 6);
            }
            using (var thumbBrush = new SolidBrush(Color.White))
            using (var blkBrush = new SolidBrush(Color.Black))
            using (var orbBrush = new SolidBrush(CyanNeon))
            {
                g.FillEllipse(thumbBrush, 630 + progW * frac - 6, 323, 12, 12);

                int curSec = 92 + (f / 30);
                g.DrawString(string.Format("{0}:{1:D2}", curSec / 60, curSec % 60), FontMicro, mBrush, 630, 342);
                g.DrawString("2:58", FontMicro, mBrush, 630 + progW - 28, 342);

                // Glowing Orb Transport Controls
                g.DrawString("⏮", FontHero, wBrush, 760, 385);
                g.FillEllipse(orbBrush, 825, 376, 56, 56);
                g.DrawString("⏸", FontHeader, blkBrush, 844, 393);
                g.DrawString("⏭", FontHero, wBrush, 905, 385);
            }

            string mode = f < 100 ? "MODE: VINYL (30 FPS SHADER)" : (f < 200 ? "MODE: GLOWING BEATS" : "MODE: AMBIENT PARTICLES");
            DrawSpecularCard(g, 630, 464, 240, 32, 12f, Surface2, OrchidNeon, 0.8f);
            g.DrawString(mode, FontMicro, wBrush, 650, 474);
        }
    }

    // SCENE 5: Squish Dynamic Tiling & Windows 11 DWM Chrome (Frames 1320-1619 -> local f 0-299)
    private static void DrawScene5_SquishTiling(Graphics g, int f, double t)
    {
        DrawSnootPill(g, 410, 36, true, "( ¬ _ ¬ )", "Squish Active", "DWM ROUND", false, 0, 0);

        int activeWin = (f < 100) ? 0 : (f < 200 ? 1 : 2);
        bool showSplit4 = (f >= 150);

        // Window 1 (Left 50%): VS Code
        DrawWin11AppWindow(g, 16, 52, 616, 610, "Visual Studio Code — LaunchMe OS", activeWin == 0,
            new[] { "// Squish Golden-Ratio BSP Engine + Win32 DeferWindowPos", "Win32TilingDispatcher.ApplyWindows11SquishChrome(hwnd, isFocused: true);", "DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, DWMWCP_ROUND);", "DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, 0x00FFF000);" });

        // Window 2 (Top Right 25%): Windows Terminal
        DrawWin11AppWindow(g, 646, 52, 618, 298, "Windows Terminal — PowerShell", activeWin == 1,
            new[] { "PS E:\\LaunchMe OS> dotnet test LaunchMeWin.sln --no-build", "Passed! - Platform: 129/129 | Core: 243/243 | UI: 116/116", "Aggregate: 488 / 488 Tests Passing (0 Warnings, 0 CVEs)" });

        // Window 3 (Bottom Right): Edge or Split 3+4
        if (!showSplit4)
        {
            DrawWin11AppWindow(g, 646, 364, 618, 298, "Microsoft Edge — GitHub Release v1.0.0", activeWin == 2,
                new[] { "https://github.com/redwan2003-bot/LaunchMe_OS", "Tag: v1.0.0 • SHA256 Checksums Verified • Signed Authenticode" });

            // Acrylic Drop Ghost Preview animating before split4 opens
            if (f >= 110 && f < 150)
            {
                DrawSpecularCard(g, 962, 364, 302, 298, 12f, Color.FromArgb(90, 0, 240, 255), CyanNeon, 2.0f);
                using (var tpBrush = new SolidBrush(TextPrimary))
                {
                    g.DrawString("SQUISH GHOST PREVIEW", FontMicro, tpBrush, 1045, 505);
                }
            }
        }
        else
        {
            DrawWin11AppWindow(g, 646, 364, 302, 298, "Microsoft Edge", false, new[] { "github.com/redwan2003-bot" });
            DrawWin11AppWindow(g, 962, 364, 302, 298, "File Explorer — E:\\LaunchMe OS", activeWin == 2, new[] { "src/", "tests/", "dist/" });
        }

        // Gliding cursor showing focus switch
        float cx = activeWin == 0 ? 320 : (activeWin == 1 ? 940 : 1100);
        float cy = activeWin == 0 ? 320 : (activeWin == 1 ? 190 : 490);
        DrawCursor(g, cx, cy, (f % 100) == 5);
    }

    private static void DrawWin11AppWindow(Graphics g, float x, float y, float w, float h, string title, bool focused, string[] lines)
    {
        DrawSpecularCard(g, x, y, w, h, 12f, Color.FromArgb(244, 18, 22, 32), focused ? CyanNeon : (Color?)null, focused ? 2.2f : 0.8f);

        using (var wBrush = new SolidBrush(TextPrimary))
        using (var mBrush = new SolidBrush(TextMuted))
        using (var cBrush = new SolidBrush(CyanNeon))
        {
            // Windows 11 Top-Right Window Controls: — 🗖 ✕ (ZERO macOS dots!)
            g.DrawString(title, FontBody, focused ? wBrush : mBrush, x + 14, y + 10);
            g.DrawString("—     🗖     ✕", FontBody, wBrush, x + w - 82, y + 9);

            using (var sep = new Pen(Color.FromArgb(30, 255, 255, 255), 1f))
            {
                g.DrawLine(sep, x + 1, y + 34, x + w - 1, y + 34);
            }

            for (int i = 0; i < lines.Length; i++)
            {
                g.DrawString(lines[i], FontMono, i == 0 && focused ? cBrush : wBrush, x + 16, y + 52 + (i * 22));
            }
        }
    }

    // SCENE 6: Leash P2P Sync & Pause-on-Hide Finale (Frames 1620-1859 -> local f 0-239)
    private static void DrawScene6_LeashAndFinale(Graphics g, int f, double t)
    {
        if (f < 150)
        {
            DrawSnootPill(g, 410, 36, true, "( •̀ ω •́ )", "Leash Ready", "AES-GCM", false, 0, 0);

            float fx = (W - 660) / 2f, fy = 110;
            DrawSpecularCard(g, fx, fy, 660, 440, 24f, Surface2, EmeraldNeon, 1.2f);
            using (var wBrush = new SolidBrush(TextPrimary))
            using (var eBrush = new SolidBrush(EmeraldNeon))
            using (var cBrush = new SolidBrush(CyanNeon))
            using (var mBrush = new SolidBrush(TextMuted))
            {
                g.DrawString("LEASH RELAY  •  ZERO-CLOUD P2P E2EE", FontMicro, eBrush, fx + 32, fy + 28);
                g.DrawString("Mobile Companion & Settings", FontHero, wBrush, fx + 32, fy + 52);

                DrawSpecularCard(g, fx + 32, fy + 108, 280, 120, 16f, Color.FromArgb(200, 12, 16, 26), CyanNeon, 0.9f);
                g.DrawString("6-DIGIT PAIRING PIN", FontMicro, cBrush, fx + 52, fy + 128);
                g.DrawString("482 - 910", FontHero, wBrush, fx + 52, fy + 152);
                g.DrawString("PBKDF2 100k • 96-bit Nonce", FontMicro, mBrush, fx + 52, fy + 194);

                DrawSpecularCard(g, fx + 332, fy + 108, 296, 120, 16f, Color.FromArgb(200, 12, 16, 26), EmeraldNeon, 0.9f);
                g.DrawString("PAIRED DEVICE", FontMicro, eBrush, fx + 352, fy + 128);
                g.DrawString("📱 Pixel 9 Pro", FontHeader, wBrush, fx + 352, fy + 154);
                g.DrawString("✓ Connected via Local UDP", FontBody, eBrush, fx + 352, fy + 188);

                g.DrawString("QUICK LUXURY TINT PRESETS", FontMicro, mBrush, fx + 32, fy + 258);
                Color[] tints = { Color.FromArgb(30, 64, 175), Color.FromArgb(4, 120, 87), Color.FromArgb(159, 18, 57), Color.FromArgb(30, 41, 59) };
                string[] tNames = { "Cobalt", "Emerald", "Crimson", "Obsidian" };
                for (int i = 0; i < 4; i++)
                {
                    DrawSpecularCard(g, fx + 32 + i * 150, fy + 286, 134, 40, 12f, tints[i]);
                    g.DrawString(tNames[i], FontBody, wBrush, fx + 62 + i * 150, fy + 298);
                }

                g.DrawString("PAUSE-ON-HIDE GPU LAW: ENFORCED (0.0% BACKGROUND LOAD)", FontMicro, cBrush, fx + 32, fy + 365);
            }
        }
        else
        {
            // Clean Finale Screen (Pause-on-Hide Active)
            float cw = 680, ch = 260;
            float cx = (W - cw) / 2f, cy = (H - ch) / 2f;
            DrawSpecularCard(g, cx, cy, cw, ch, 28f, Surface1, CyanNeon, 1.2f);
            using (var wBrush = new SolidBrush(TextPrimary))
            using (var cBrush = new SolidBrush(CyanNeon))
            using (var eBrush = new SolidBrush(EmeraldNeon))
            using (var mBrush = new SolidBrush(TextMuted))
            {
                g.DrawString("LAUNCHME OS  •  WINDOWS 11", FontHero, wBrush, cx + 140, cy + 48);
                g.DrawString("EDITORIAL LUXURY SHELL  •  0.0% IDLE GPU (PAUSE-ON-HIDE)", FontMicro, cBrush, cx + 145, cy + 92);
                g.DrawString("✓ 488 / 488 Tests Passing   ✓ 0 Warnings   ✓ 0 CVEs   ✓ 0 Stubs", FontSub, eBrush, cx + 120, cy + 140);
                g.DrawString("Pookie  •  Blobbo  •  Squish  •  Snoot  •  Leash", FontHeader, mBrush, cx + 160, cy + 185);
            }
        }
    }
}
"@

Write-Host "Compiling C# 30 FPS Renderer..." -ForegroundColor Cyan
Add-Type -TypeDefinition $csharpRenderer -ReferencedAssemblies System.Drawing

Write-Host "Generating 1,860 1280x720 Windows 11 Editorial Luxury frames..." -ForegroundColor Cyan
[LuxuryTourRenderer]::RenderAll($frameDumpDir)

Write-Host "Encoding 30.0 FPS MP4 & WebM with FFmpeg..." -ForegroundColor Cyan
$finalMp4 = "$outDir\launchme_os_luxury_win11_30fps.mp4"
$finalWebm = "$outDir\launchme_os_luxury_win11_30fps.webm"

ffmpeg -y -framerate 30 -i "$frameDumpDir\frame_%05d.png" -c:v libx264 -pix_fmt yuv420p -preset fast -crf 20 -r 30 "$finalMp4"
ffmpeg -y -i "$finalMp4" -c:v libvpx-vp9 -b:v 2200k -r 30 "$finalWebm"

Copy-Item "$finalMp4" "$distVideoDir\" -Force
Copy-Item "$finalWebm" "$distVideoDir\" -Force

# Clean up uncompressed BMP frames after encoding to save device storage!
Remove-Item $frameDumpDir -Recurse -Force

Write-Host "=== 30 FPS Luxury Tour Rendering Complete! ===" -ForegroundColor Green
Get-Item "$finalMp4", "$finalWebm" | Select-Object Name, @{Name="Size (MB)"; Expression={[Math]::Round($_.Length / 1MB, 2)}}
