$ErrorActionPreference = "Stop"

$baseDir = "C:\Users\Redwan Ahmmed\.gemini\antigravity\brain\89c20678-0592-4161-88c5-7b793ab68dac"
$outDir = "$baseDir\videos"
$frameDumpDir = "$outDir\win11_v3_frames"
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

public static class LaunchMeV3TourRenderer
{
    private const int W = 1280;
    private const int H = 720;

    // Editorial Luxury Obsidian Palette
    private static readonly Color ObsidianVoid = Color.FromArgb(8, 10, 15);
    private static readonly Color Surface1 = Color.FromArgb(224, 14, 19, 31);
    private static readonly Color Surface2 = Color.FromArgb(240, 22, 29, 46);
    private static readonly Color CyanNeon = Color.FromArgb(0, 240, 255);
    private static readonly Color OrchidNeon = Color.FromArgb(168, 85, 247);
    private static readonly Color EmeraldNeon = Color.FromArgb(0, 255, 157);
    private static readonly Color AmberTopaz = Color.FromArgb(245, 158, 11);
    private static readonly Color RoseCrimson = Color.FromArgb(244, 63, 94);
    private static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);
    private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);

    private static Font FontHero;
    private static Font FontMonogram;
    private static Font FontHeader;
    private static Font FontSub;
    private static Font FontBody;
    private static Font FontMicro;
    private static Font FontMono;
    private static Font FontEmoji;

    public static void RenderAll(string outDir)
    {
        FontHero = new Font("Segoe UI Variable Display", 24f, FontStyle.Bold, GraphicsUnit.Pixel);
        FontMonogram = new Font("Segoe UI Variable Display", 78f, FontStyle.Bold, GraphicsUnit.Pixel);
        FontHeader = new Font("Segoe UI Variable Display", 17f, FontStyle.Bold, GraphicsUnit.Pixel);
        FontSub = new Font("Segoe UI Variable Text", 13f, FontStyle.Regular, GraphicsUnit.Pixel);
        FontBody = new Font("Segoe UI Variable Text", 11.5f, FontStyle.Regular, GraphicsUnit.Pixel);
        FontMicro = new Font("Segoe UI Variable Small", 9.5f, FontStyle.Bold, GraphicsUnit.Pixel);
        FontMono = new Font("Cascadia Code", 11f, FontStyle.Regular, GraphicsUnit.Pixel);
        FontEmoji = new Font("Segoe UI Emoji", 13f, FontStyle.Regular, GraphicsUnit.Pixel);

        int totalFrames = 1860; // 62.0 seconds @ exact 30.0 FPS

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

                if (f < 150)
                {
                    DrawScene0_LoadingBoot(g, f, t);
                }
                else if (f < 450)
                {
                    DrawScene1_LauncherAndClocks(g, f - 150, t);
                }
                else if (f < 840)
                {
                    DrawScene2_Notch10on10(g, f - 450, t);
                }
                else if (f < 1080)
                {
                    DrawScene3_DockAndAudioDsp(g, f - 840, t);
                }
                else if (f < 1440)
                {
                    DrawScene4_MusicPlayer4Modes(g, f - 1080, t);
                }
                else if (f < 1650)
                {
                    DrawScene5_SquishAndFlow(g, f - 1440, t);
                }
                else
                {
                    DrawScene6_LocalesLeashAndFinale(g, f - 1650, t);
                }

                if (f >= 120 && f < 1800)
                {
                    DrawWindows11Taskbar(g);
                }

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
            using (var shadowPath = CreateSquirclePath(x, y + 5, w, h, r))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(85, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }
            using (var fillBrush = new SolidBrush(fill))
            {
                g.FillPath(fillBrush, path);
            }
            Color cTopLeft = highlightBorder.HasValue ? Color.FromArgb(205, highlightBorder.Value) : Color.FromArgb(145, 255, 255, 255);
            Color cBotRight = highlightBorder.HasValue ? Color.FromArgb(40, highlightBorder.Value) : Color.FromArgb(16, 255, 255, 255);
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

        float cx1 = 360f + (float)(Math.Sin(t * 0.4) * 90);
        float cy1 = 220f + (float)(Math.Cos(t * 0.3) * 60);
        using (var gp1 = new GraphicsPath())
        {
            gp1.AddEllipse(cx1 - 380, cy1 - 380, 760, 760);
            using (var pgb1 = new PathGradientBrush(gp1))
            {
                pgb1.CenterColor = Color.FromArgb(44, 0, 120, 212);
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
                pgb2.CenterColor = Color.FromArgb(34, 168, 85, 247);
                pgb2.SurroundColors = new[] { Color.FromArgb(0, 8, 10, 15) };
                g.FillPath(pgb2, gp2);
            }
        }
    }

    private static void DrawCursor(Graphics g, float x, float y, bool clicking = false)
    {
        if (clicking)
        {
            using (var clickPen = new Pen(Color.FromArgb(160, CyanNeon), 2.2f))
            {
                g.DrawEllipse(clickPen, x - 13, y - 13, 26, 26);
            }
        }
        PointF[] pts = {
            new PointF(x, y), new PointF(x, y + 18), new PointF(x + 5, y + 14),
            new PointF(x + 9, y + 21), new PointF(x + 12, y + 19), new PointF(x + 8, y + 12), new PointF(x + 14, y + 12)
        };
        using (var fill = new SolidBrush(Color.White))
        using (var stroke = new Pen(Color.Black, 1.5f))
        {
            g.FillPolygon(fill, pts);
            g.DrawPolygon(stroke, pts);
        }
    }

    private static void DrawWindows11Taskbar(Graphics g)
    {
        int tbH = 44;
        int tbY = H - tbH;
        using (var tbBrush = new SolidBrush(Color.FromArgb(232, 10, 13, 20)))
            g.FillRectangle(tbBrush, 0, tbY, W, tbH);
        using (var topLine = new Pen(Color.FromArgb(38, 255, 255, 255), 1f))
            g.DrawLine(topLine, 0, tbY, W, tbY);

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
                g.DrawString(icons[i], FontEmoji, tbText, startX + 34 + (i * 32), tbY + 11);
        }

        using (var pill = new SolidBrush(CyanNeon))
            g.FillRectangle(pill, startX + 34 + (5 * 32) + 4, H - 5, 12, 3);

        using (var trayBrush = new SolidBrush(TextMuted))
            g.DrawString("ENG   📶   🔊   100%   17:38", FontBody, trayBrush, W - 200, tbY + 14);
    }

    private static void DrawSnootPill(Graphics g, float width, float height, string track, string timer, bool focusOn, bool pulseAlert, float pupilDx, float pupilDy)
    {
        float x = (W - width) / 2f;
        float y = 8f;
        Color? borderHighlight = pulseAlert ? AmberTopaz : (focusOn ? OrchidNeon : (Color?)null);
        DrawSpecularCard(g, x, y, width, height, 20f, Color.FromArgb(244, 8, 10, 15), borderHighlight, pulseAlert ? 1.5f : 0.8f);

        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var aBrush = new SolidBrush(AmberTopaz))
        using (var oBrush = new SolidBrush(OrchidNeon))
        using (var mBrush = new SolidBrush(TextMuted))
        {
            g.DrawString("22°C CLEAR", FontMicro, cBrush, x + 16, y + 11);

            float bx = x + 98;
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

            g.DrawString(track, FontBody, wBrush, x + 134, y + 9);

            if (focusOn)
            {
                DrawSpecularCard(g, x + width - 152, y + 8, 50, 20, 8f, Color.FromArgb(70, 168, 85, 247), OrchidNeon, 0.8f);
                g.DrawString("FOCUS", FontMicro, oBrush, x + width - 143, y + 12);
            }

            g.DrawString(timer + " • 100%", FontMicro, pulseAlert ? aBrush : mBrush, x + width - 94, y + 11);
        }
    }

    private static void DrawHudWatermark(Graphics g, int frame, double t)
    {
        DrawSpecularCard(g, W - 265, 10, 250, 28, 12f, Color.FromArgb(220, 8, 10, 15), CyanNeon, 0.7f);
        using (var redDot = new SolidBrush(Color.FromArgb(239, 68, 68)))
        using (var txtBrush = new SolidBrush(TextPrimary))
        {
            if ((frame / 15) % 2 == 0)
                g.FillEllipse(redDot, W - 253, 20, 7, 7);
            g.DrawString(string.Format("30.0 FPS  •  LAUNCHME v3.01  •  {0:00}:{1:00}", (int)t / 60, (int)t % 60), FontMicro, txtBrush, W - 238, 18);
        }
    }

    // SCENE 0: Cold Boot & Full Loading Sequence (Frames 0-149)
    private static void DrawScene0_LoadingBoot(Graphics g, int f, double t)
    {
        float progress = Math.Min(1.0f, f / 115.0f);
        float cw = 590, ch = 390;
        float cx = (W - cw) / 2f, cy = (H - ch) / 2f;
        DrawSpecularCard(g, cx, cy, cw, ch, 26f, Surface1, CyanNeon, 1.1f);

        using (var tBrush = new SolidBrush(TextPrimary))
        using (var mBrush = new SolidBrush(TextMuted))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var eBrush = new SolidBrush(EmeraldNeon))
        {
            g.DrawString("LAUNCHME OS", FontHero, tBrush, cx + 36, cy + 30);
            g.DrawString("WINDOWS 11 WORKSPACE SHELL  •  v3.01-WIN-LUXURY", FontMicro, cBrush, cx + 38, cy + 64);

            float spinAngle = (f * 14) % 360;
            using (var trackPen = new Pen(Color.FromArgb(40, 255, 255, 255), 3f))
            using (var arcPen = new Pen(CyanNeon, 3f))
            {
                g.DrawEllipse(trackPen, cx + cw - 82, cy + 32, 42, 42);
                g.DrawArc(arcPen, cx + cw - 82, cy + 32, 42, 42, spinAngle, 125);
            }

            string[] steps = {
                "Contracts, SqliteSpaceStore & .launchmetheme Shell Association",
                "Win32ShellIconService (256px Jumbo + MSIX Vector + G2 Pedestal)",
                "Win2D LiquidGlassCompositor & 12 Luxury Clock XAML Skins",
                "Snoot HUD (Clipboard Cards, Nearby Share, Focus, Size Sliders)",
                "Music Player (Vinyl, Full Art, Particles, Glowing Beats) & AudioDSP",
                "LocalizationService (18 Translated Locales) + 549/549 Tests Green"
            };

            for (int i = 0; i < steps.Length; i++)
            {
                bool done = progress >= ((i + 1) / (float)steps.Length);
                string prefix = done ? "[✓]  " : "[•]  ";
                g.DrawString(prefix + steps[i], FontBody, done ? eBrush : mBrush, cx + 38, cy + 112 + (i * 30));
            }

            float barW = cw - 76;
            DrawSpecularCard(g, cx + 38, cy + ch - 64, barW, 10, 5f, Color.FromArgb(90, 255, 255, 255));
            if (progress > 0.02f)
            {
                using (var fillBar = new LinearGradientBrush(new PointF(cx + 38, 0), new PointF(cx + 38 + barW, 0), CyanNeon, OrchidNeon))
                using (var p = CreateSquirclePath(cx + 38, cy + ch - 64, Math.Max(10, barW * progress), 10, 5f))
                {
                    g.FillPath(fillBar, p);
                }
            }
            g.DrawString(string.Format("INITIALIZING SURFACES & VERIFYING HOOKS... {0}%", (int)(progress * 100)), FontMicro, mBrush, cx + 38, cy + ch - 40);
        }

        if (f >= 115)
        {
            float sf = (f - 115) / 35.0f;
            float springEase = (float)(1.0 - Math.Exp(-6.0 * sf) * Math.Cos(12.0 * sf));
            float dropW = 430 * Math.Min(1.05f, springEase);
            DrawSnootPill(g, Math.Max(110, dropW), 36, "Midnight City • M83", "25:00", true, false, 0, 0);
        }
    }

    // SCENE 1: Full-Screen Launcher + 256px G2 Icons + 12 Clock Skins Carousel (Frames 150-449 -> local f 0-299)
    private static void DrawScene1_LauncherAndClocks(Graphics g, int f, double t)
    {
        DrawSnootPill(g, 430, 36, "Midnight City • M83", "24:58", true, false, 0, 0);

        float sx = (W - 650) / 2f, sy = 64;
        DrawSpecularCard(g, sx, sy, 650, 48, 18f, Surface2, CyanNeon, 1.2f);

        string typedQuery = "= 2 * pi * 64";
        int chars = Math.Min(typedQuery.Length, f / 6);
        string shownText = f < 150 ? typedQuery.Substring(0, chars) : "pixel art  [TAB: EMOJI]";

        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var oBrush = new SolidBrush(OrchidNeon))
        using (var mBrush = new SolidBrush(TextMuted))
        using (var eBrush = new SolidBrush(EmeraldNeon))
        {
            g.DrawString("🔍  " + shownText + ((f / 12) % 2 == 0 ? "|" : ""), FontSub, wBrush, sx + 18, sy + 14);
            if (f >= 75 && f < 150)
            {
                DrawSpecularCard(g, sx + 455, sy + 9, 178, 30, 10f, Color.FromArgb(40, 0, 240, 255), CyanNeon, 0.8f);
                g.DrawString("CALC: 402.1238", FontMicro, cBrush, sx + 474, sy + 18);
            }
            else
            {
                DrawSpecularCard(g, sx + 530, sy + 9, 106, 30, 10f, Color.FromArgb(40, 168, 85, 247), OrchidNeon, 0.8f);
                g.DrawString("EMOJI (TAB)", FontMicro, oBrush, sx + 548, sy + 18);
            }

            // Left Bento 1: Solar Weather + Pixel Art Animated Pet
            DrawSpecularCard(g, 90, 136, 310, 180, 24f, Surface1);
            g.DrawString("SOLAR WEATHER + PIXEL PET (LOOP)", FontMicro, cBrush, 112, 156);
            g.DrawString("22°C", FontHero, wBrush, 112, 182);
            g.DrawString("Clear • Feels 21°C • 52% RH", FontBody, mBrush, 112, 222);
            // Animated Pixel Cat / Slime 5x5 grid cycling frames at ~3fps
            int pxFrame = (f / 10) % 4;
            Color pxColor = (f / 40) % 2 == 0 ? CyanNeon : AmberTopaz;
            using (var pxBrush = new SolidBrush(pxColor))
            {
                for (int r = 0; r < 5; r++)
                {
                    for (int c = 0; c < 5; c++)
                    {
                        if (((r + c + pxFrame) % 2 == 0) || (r >= 2 && c >= 1 && c <= 3))
                            g.FillRectangle(pxBrush, 310 + c * 11, 180 + r * 11, 9, 9);
                    }
                }
            }
            g.DrawString("18 Animated Presets • 0% Idle GPU", FontMicro, eBrush, 112, 276);

            // Center Bento 2: 12 CLOCK SKINS LIVE CAROUSEL
            string[] clockSkins = {
                "DigitalMinimal", "DigitalBold", "DigitalSquared", "AnalogClassic",
                "AnalogMinimal", "AnalogChrono", "FlipSplitFlap", "SwissRail",
                "EditorialSerif", "ConcentricRing", "NeonBinary", "WorldDualTime"
            };
            int skinIdx = (f / 25) % 12;
            string skinName = clockSkins[skinIdx];
            Color skinAccent = skinIdx % 3 == 0 ? CyanNeon : (skinIdx % 3 == 1 ? OrchidNeon : AmberTopaz);
            DrawSpecularCard(g, 420, 136, 310, 180, 24f, Surface2, skinAccent, 1.2f);
            g.DrawString(string.Format("12 LUXURY CLOCK SKINS ({0}/12)", skinIdx + 1), FontMicro, cBrush, 442, 156);
            g.DrawString("SKIN: " + skinName, FontMicro, oBrush, 442, 174);
            if (skinName.Contains("Analog") || skinName == "ConcentricRing")
            {
                // Draw clock dial
                using (var ringPen = new Pen(skinAccent, 2.5f))
                using (var handPen = new Pen(TextPrimary, 2.5f))
                {
                    g.DrawEllipse(ringPen, 535, 196, 80, 80);
                    float secAng = (float)((f * 6.0) * Math.PI / 180.0);
                    g.DrawLine(handPen, 575, 236, 575 + (float)Math.Cos(secAng) * 28, 236 + (float)Math.Sin(secAng) * 28);
                    g.DrawLine(ringPen, 575, 236, 575, 210);
                }
            }
            else if (skinName == "FlipSplitFlap")
            {
                string[] digits = { "1", "7", ":", "3", "8" };
                for (int d = 0; d < 5; d++)
                {
                    if (d == 2) { g.DrawString(":", FontHero, mBrush, 442 + d * 42, 212); continue; }
                    DrawSpecularCard(g, 442 + d * 42, 206, 36, 48, 6f, Color.FromArgb(255, 18, 20, 28), AmberTopaz, 0.8f);
                    g.DrawString(digits[d], FontHeader, new SolidBrush(AmberTopaz), 453 + d * 42, 218);
                }
            }
            else if (skinName == "WorldDualTime")
            {
                g.DrawString("17:38", FontHero, wBrush, 442, 206);
                g.DrawString("LOCAL", FontMicro, mBrush, 442, 242);
                g.DrawString("11:38", FontHero, cBrush, 565, 206);
                g.DrawString("UTC", FontMicro, cBrush, 565, 242);
            }
            else
            {
                g.DrawString("17:38:42", FontHero, skinName == "NeonBinary" ? eBrush : wBrush, 442, 208);
                g.DrawString("Thursday, November 26", FontBody, mBrush, 442, 250);
            }

            // Right Bento 3: 11-Family Catalog
            DrawSpecularCard(g, 750, 136, 440, 480, 24f, Surface2, OrchidNeon, 0.9f);
            g.DrawString("11-FAMILY WIDGET CATALOG", FontMicro, oBrush, 774, 158);
            string[] wNames = { "12 Clock Skins", "Weather Card", "GSMTC Music", "System Stats", "Focus Timer", "Affirmations", "Calendar", "18 Pixel Pets", "Photo Frame", "Shape Mask", "GIF Loop" };
            for (int i = 0; i < wNames.Length; i++)
            {
                int col = i % 2, row = i / 2;
                float wx = 774 + col * 200, wy = 190 + row * 66;
                bool hi = (f / 22) % wNames.Length == i;
                DrawSpecularCard(g, wx, wy, 188, 52, 14f, Color.FromArgb(190, 18, 24, 38), hi ? CyanNeon : (Color?)null, hi ? 1.2f : 0.6f);
                g.DrawString(wNames[i], FontBody, wBrush, wx + 14, wy + 17);
            }

            // Bottom 256px Shell/MSIX Icons on G2 Squircle Pedestals
            g.DrawString("256PX JUMBO SHELL + MSIX VECTOR ICONS ON G2 SQUIRCLE PEDESTALS", FontMicro, cBrush, 90, 336);
            string[] apps = { "Terminal", "VS Code", "Explorer", "Edge", "Spotify", "Settings" };
            string[] appIcons = { "💻", "📝", "📁", "🌐", "🎵", "⚙️" };
            int hovered = (f / 35) % apps.Length;
            for (int i = 0; i < apps.Length; i++)
            {
                float ax = 90 + i * 108, ay = 362;
                bool isH = (i == hovered);
                DrawSpecularCard(g, ax, ay, 88, 88, 24f, isH ? Surface2 : Surface1, isH ? CyanNeon : (Color?)null, isH ? 1.6f : 0.8f);
                g.DrawString(appIcons[i], FontHero, wBrush, ax + 25, ay + 22);
                g.DrawString(apps[i], FontBody, isH ? wBrush : mBrush, ax + 16, ay + 96);
            }

            // Space & Workflows Bar
            DrawSpecularCard(g, 90, 510, 640, 106, 22f, Surface1);
            g.DrawString("UNIFIED SPACES  •  WORKFLOWS  •  .LAUNCHMETHEME ZIP", FontMicro, eBrush, 114, 530);
            g.DrawString("Space: Dev Studio     Workflows: [ Launch IDE Stack ]  [ Deep Focus ]", FontBody, wBrush, 114, 560);
        }

        DrawCursor(g, 134 + ((f / 35) % 6) * 108, 404 + (float)(Math.Sin(f * 0.1) * 6), (f % 35) == 15);
    }

    // SCENE 2: Dynamic Notch (Snoot HUD) 10/10 Upgrade (Frames 450-839 -> local f 0-389)
    // Shows 4 sub-acts:
    //  0-99: Pocket & Pookie Approval Cockpit + Blobbo eye gaze
    // 100-199: Clipboard Card UI (7 category chips, pin star, copy button)
    // 200-289: Size & HUD Dimension Sliders resizing the Notch live
    // 290-389: Split Drop-Zone Overlay (DROP TO POCKET + WINDOWS NEARBY SHARE)
    private static void DrawScene2_Notch10on10(Graphics g, int f, double t)
    {
        float cursorX = 640 + (float)(Math.Sin(t * 2.1) * 260);
        float cursorY = 520 + (float)(Math.Cos(t * 1.5) * 60);
        float pupilDx = Math.Max(-2.5f, Math.Min(2.5f, (cursorX - 640) / 140f));
        float pupilDy = Math.Max(-2.0f, Math.Min(2.0f, (cursorY - 80) / 180f));

        // Dynamic size slider animation during act 3 (f 200-289)
        float baseW = 660, baseH = 440;
        if (f >= 200 && f < 290)
        {
            float sf = (f - 200) / 89.0f;
            float wave = (float)Math.Sin(sf * Math.PI);
            baseW = 660 + wave * 110;
            baseH = 440 + wave * 60;
        }
        float drawX = (W - baseW) / 2f;
        bool alertBorder = (f < 100);

        DrawSpecularCard(g, drawX, 12, baseW, baseH, 24f, Surface2, alertBorder ? AmberTopaz : CyanNeon, 1.3f);

        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var oBrush = new SolidBrush(OrchidNeon))
        using (var eBrush = new SolidBrush(EmeraldNeon))
        using (var aBrush = new SolidBrush(AmberTopaz))
        using (var mBrush = new SolidBrush(TextMuted))
        {
            // Compact Header Row
            g.DrawString("SNOOT HUD  10/10", FontMicro, cBrush, drawX + 20, 24);
            g.DrawString("Midnight City • 24:50", FontBody, wBrush, drawX + baseW / 2 - 75, 22);
            DrawSpecularCard(g, drawX + baseW - 140, 20, 52, 20, 8f, Color.FromArgb(70, 168, 85, 247), OrchidNeon, 0.8f);
            g.DrawString("FOCUS", FontMicro, oBrush, drawX + baseW - 131, 24);
            g.DrawString("100%", FontMicro, eBrush, drawX + baseW - 60, 24);

            // Tab Bar
            int activeTab = f < 100 ? 0 : (f < 200 ? 1 : (f < 290 ? 2 : 3));
            string[] tabs = { "Pocket & Share", "Clipboard (7 Cats)", "Size & Focus Sliders" };
            for (int ti = 0; ti < 3; ti++)
            {
                bool sel = (activeTab == ti || (activeTab == 3 && ti == 0));
                DrawSpecularCard(g, drawX + 30 + ti * 200, 58, 186, 30, 10f,
                    sel ? Color.FromArgb(45, 0, 240, 255) : Color.FromArgb(20, 255, 255, 255),
                    sel ? CyanNeon : (Color?)null, 0.9f);
                g.DrawString(tabs[ti], FontBody, sel ? cBrush : wBrush, drawX + 54 + ti * 200, 65);
            }

            if (activeTab == 0)
            {
                // Act 1: Pookie Approval + Blobbo + Pocket Shelf
                DrawSpecularCard(g, drawX + 28, 102, baseW - 56, 175, 16f, Color.FromArgb(220, 12, 16, 26), AmberTopaz, 1.2f);
                g.DrawString(f > 70 ? "✓ POOKIE ALLOWED" : "⚠ POOKIE PERMISSION ALERT", FontMicro, f > 70 ? eBrush : aBrush, drawX + 44, 118);
                g.DrawString("Command: git push origin main --tags", FontSub, wBrush, drawX + 44, 140);
                DrawSpecularCard(g, drawX + 44, 168, baseW - 88, 52, 8f, Color.FromArgb(240, 6, 8, 12));
                g.DrawString("+ feat(ui): upgrade Notch Clipboard, Nearby Share & 18 Locales", FontMono, eBrush, drawX + 56, 182);
                DrawSpecularCard(g, drawX + 44, 230, 88, 28, 8f, Color.FromArgb(200, 22, 163, 74), EmeraldNeon, 1.0f);
                g.DrawString("Allow", FontBody, wBrush, drawX + 70, 236);
                DrawSpecularCard(g, drawX + 144, 230, 88, 28, 8f, Color.FromArgb(180, 220, 38, 38));
                g.DrawString("Deny", FontBody, wBrush, drawX + 172, 236);

                DrawSpecularCard(g, drawX + 28, 290, baseW - 56, 130, 16f, Surface1);
                g.DrawString("SNOOT POCKET SHELF (READY TO DRAG OR SHARE)", FontMicro, oBrush, drawX + 44, 306);
                g.DrawString("📄  launchme_v3.01_spec.pdf       🖼️  hero_showcase.png", FontBody, wBrush, drawX + 44, 342);
            }
            else if (activeTab == 1)
            {
                // Act 2: CLIPBOARD CARD UI WITH 7 CATEGORY CHIPS
                string[] cats = { "All", "Images", "Colors", "Code", "Text", "Files", "Favorites" };
                int catHi = ((f - 100) / 16) % cats.Length;
                for (int ci = 0; ci < cats.Length; ci++)
                {
                    bool cSel = (ci == catHi);
                    DrawSpecularCard(g, drawX + 28 + ci * 86, 102, 78, 26, 8f,
                        cSel ? Color.FromArgb(50, 0, 240, 255) : Color.FromArgb(24, 255, 255, 255),
                        cSel ? CyanNeon : (Color?)null, 0.8f);
                    g.DrawString(cats[ci], FontMicro, cSel ? cBrush : mBrush, drawX + 44 + ci * 86, 109);
                }

                string[,] cards = {
                    { "CODE", "dotnet build -warnaserror && dotnet test", "WindowsTerminal.exe" },
                    { "COLORS", "#00F0FF  •  Obsidian Liquid Glass Accent", "Figma.exe" },
                    { "FILES", "launchme_installer_win11_x64.msix (104 MB)", "explorer.exe" }
                };
                for (int r = 0; r < 3; r++)
                {
                    float cy = 142 + r * 92;
                    DrawSpecularCard(g, drawX + 28, cy, baseW - 56, 80, 14f, Surface1, r == 0 ? CyanNeon : (Color?)null, 0.8f);
                    DrawSpecularCard(g, drawX + 42, cy + 12, 70, 20, 6f, Color.FromArgb(40, 0, 240, 255), CyanNeon, 0.7f);
                    g.DrawString(cards[r, 0], FontMicro, cBrush, drawX + 52, cy + 16);
                    g.DrawString(cards[r, 2], FontMicro, mBrush, drawX + 124, cy + 16);
                    g.DrawString(cards[r, 1], FontSub, wBrush, drawX + 42, cy + 44);
                    DrawSpecularCard(g, drawX + baseW - 140, cy + 24, 34, 28, 8f, Color.FromArgb(40, 168, 85, 247), OrchidNeon, 0.7f);
                    g.DrawString("★", FontSub, oBrush, drawX + baseW - 130, cy + 29);
                    DrawSpecularCard(g, drawX + baseW - 96, cy + 24, 52, 28, 8f, Color.FromArgb(40, 0, 255, 157), EmeraldNeon, 0.7f);
                    g.DrawString("Copy", FontMicro, eBrush, drawX + baseW - 83, cy + 32);
                }
            }
            else if (activeTab == 2)
            {
                // Act 3: LIVE SIZE & HUD DIMENSION SLIDERS
                g.DrawString("HUD DIMENSIONS & FOCUS SLIDERS (LIVE RESIZE)", FontMicro, cBrush, drawX + 34, 112);
                string[] labels = { "Compact Width (280–540 px)", "Compact Height (28–56 px)", "Drawer Width (440–760 px)", "Drawer Height (280–540 px)" };
                float[] vals = { 0.45f, 0.35f, (baseW - 440f) / 320f, (baseH - 280f) / 260f };
                for (int si = 0; si < 4; si++)
                {
                    float sy = 150 + si * 64;
                    g.DrawString(labels[si], FontSub, wBrush, drawX + 34, sy);
                    DrawSpecularCard(g, drawX + 300, sy + 6, baseW - 350, 8, 4f, Color.FromArgb(60, 255, 255, 255));
                    float fill = Math.Max(0.1f, Math.Min(0.95f, vals[si]));
                    using (var b = new LinearGradientBrush(new PointF(drawX + 300, 0), new PointF(drawX + baseW - 50, 0), CyanNeon, OrchidNeon))
                    {
                        g.FillRectangle(b, drawX + 300, sy + 6, (baseW - 350) * fill, 8);
                    }
                    g.FillEllipse(wBrush, drawX + 300 + (baseW - 350) * fill - 7, sy + 3, 14, 14);
                }
            }
            else
            {
                // Act 4: SPLIT DROP-ZONE OVERLAY (DROP TO POCKET vs WINDOWS NEARBY SHARE)
                float halfW = (baseW - 68) / 2f;
                DrawSpecularCard(g, drawX + 28, 108, halfW, 300, 18f, Color.FromArgb(60, 168, 85, 247), OrchidNeon, 1.8f);
                g.DrawString("DROP TO POCKET", FontHeader, oBrush, drawX + 95, 220);
                g.DrawString("Hold files cleanly on shelf", FontBody, mBrush, drawX + 95, 255);

                DrawSpecularCard(g, drawX + 40 + halfW, 108, halfW, 300, 18f, Color.FromArgb(60, 0, 240, 255), CyanNeon, 1.8f);
                g.DrawString("NEARBY SHARE", FontHeader, cBrush, drawX + 110 + halfW, 220);
                g.DrawString("Windows DataTransferManager", FontBody, mBrush, drawX + 90 + halfW, 255);
            }
        }

        if (f >= 290)
        {
            float dx = 640 + (float)(Math.Sin((f - 290) * 0.08) * 140);
            DrawCursor(g, dx, 240, (f % 40) == 10);
        }
        else
        {
            DrawCursor(g, cursorX, cursorY, (f == 68 || f == 140));
        }
    }

    // SCENE 3: Floating Capsule Dock & 9-Band Procedural AudioDSP Waveform (Frames 840-1079 -> local f 0-239)
    private static void DrawScene3_DockAndAudioDsp(Graphics g, int f, double t)
    {
        DrawSnootPill(g, 430, 36, "Midnight City • M83", "24:42", true, false, 0, 0);

        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var eBrush = new SolidBrush(EmeraldNeon))
        using (var oBrush = new SolidBrush(OrchidNeon))
        using (var mBrush = new SolidBrush(TextMuted))
        {
            DrawSpecularCard(g, 210, 110, 860, 390, 24f, Surface1, CyanNeon, 1.0f);
            g.DrawString("FLOATING CAPSULE DOCK  •  PROCEDURAL AUDIODSP + MULTI-DISPLAY", FontMicro, cBrush, 244, 140);
            g.DrawString("9-Band AudioGraph Waveform Synthesis (Zero .m4a Assets)", FontHero, wBrush, 244, 168);

            // Live 9-Band Procedural AudioDSP Spectrum Card inside Dock Showcase
            string[] presets = { "🌧️ RAIN SOUNDSCAPE", "🌊 OCEAN WAVES", "🌲 NIGHT FOREST", "☕ TOKYO CAFE" };
            int pIdx = (f / 60) % presets.Length;
            DrawSpecularCard(g, 244, 224, 792, 160, 18f, Color.FromArgb(220, 10, 14, 24), OrchidNeon, 1.0f);
            g.DrawString("ACTIVE DSP PRESET:  " + presets[pIdx] + "   •   SAMPLE RATE: 48 kHz STEREO PCM", FontMicro, oBrush, 268, 244);

            for (int b = 0; b < 9; b++)
            {
                float barH = 20f + (float)(Math.Abs(Math.Sin(t * 6.5 + b * 0.65)) * 78f);
                float bx = 280 + b * 82;
                using (var br = new LinearGradientBrush(new PointF(bx, 270), new PointF(bx, 370), CyanNeon, OrchidNeon))
                {
                    g.FillRectangle(br, bx, 366 - barH, 48, barH);
                }
            }
            g.DrawString("Reserving Desktop Work Area cleanly via Win32 SHAppBarMessage  •  Gaussian Hover Scale 1.45x", FontBody, mBrush, 244, 412);

            // Dock Capsule at Bottom
            string[] spaces = { "DEFAULT DOCK", "WORK / CODE", "CREATOR STUDIO" };
            int activeSpace = (f / 80) % 3;
            float dw = 880, dh = 76;
            float dx = (W - dw) / 2f, dy = H - 44 - dh - 14;
            DrawSpecularCard(g, dx, dy, dw, dh, 26f, Color.FromArgb(236, 11, 15, 23), null, 0.9f);
            g.DrawString("◀ " + spaces[activeSpace] + " ▶", FontMicro, mBrush, dx + 22, dy + 31);

            float cursorX = dx + 195 + (float)((0.5 + 0.5 * Math.Sin(t * 1.8)) * 310);
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

            DrawSpecularCard(g, dx + 548, dy + 22, 64, 32, 10f, Color.FromArgb(180, 16, 22, 34), EmeraldNeon, 0.7f);
            g.DrawString("CPU 12%", FontMicro, eBrush, dx + 556, dy + 32);
            DrawSpecularCard(g, dx + 620, dy + 22, 66, 32, 10f, Color.FromArgb(180, 16, 22, 34), AmberTopaz, 0.7f);
            g.DrawString("RAM 4.6G", FontMicro, new SolidBrush(AmberTopaz), dx + 627, dy + 32);
            DrawSpecularCard(g, dx + 694, dy + 22, 164, 32, 10f, Color.FromArgb(180, 16, 22, 34), OrchidNeon, 0.7f);
            g.DrawString("DSP: " + presets[pIdx], FontMicro, oBrush, dx + 706, dy + 32);

            DrawCursor(g, cursorX, dy + 36, false);
        }
    }

    // SCENE 4: Full-Screen Music Player 10/10 — All 4 Cinematic Modes (Frames 1080-1439 -> local f 0-359)
    //   0–89: Vinyl (spinning grooves)
    //  90–179: Full Screen Art (breathing scale + parallax monogram sleeve)
    // 180–269: Particles (18 orbiting constellation nodes)
    // 270–359: Glowing Beats (16-band spectrum + pulse ring)
    private static void DrawScene4_MusicPlayer4Modes(Graphics g, int f, double t)
    {
        g.Clear(Color.FromArgb(5, 7, 11));
        DrawSnootPill(g, 430, 36, "Midnight City • M83", "PLAYING", false, false, 0, 0);

        int mode = f / 90; // 0=Vinyl, 1=FullArt, 2=Particles, 3=GlowingBeats
        float pulse = 0.5f + 0.5f * (float)Math.Sin(t * 4.5);

        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var oBrush = new SolidBrush(OrchidNeon))
        using (var eBrush = new SolidBrush(EmeraldNeon))
        using (var mBrush = new SolidBrush(TextMuted))
        {
            // Top right pills
            DrawSpecularCard(g, W - 310, 62, 110, 30, 12f, Surface2, CyanNeon, 0.8f);
            g.DrawString("Widescreen", FontMicro, cBrush, W - 286, 70);
            DrawSpecularCard(g, W - 188, 62, 128, 30, 12f, Surface2, OrchidNeon, 0.8f);
            g.DrawString("Art Sync: ON", FontMicro, oBrush, W - 160, 70);

            float cx = 360, cy = 340;

            if (mode == 0)
            {
                // 1) VINYL MODE
                using (var haloPath = new GraphicsPath())
                {
                    haloPath.AddEllipse(cx - 210 - pulse * 18, cy - 210 - pulse * 18, 420 + pulse * 36, 420 + pulse * 36);
                    using (var pgb = new PathGradientBrush(haloPath))
                    {
                        pgb.CenterColor = Color.FromArgb((int)(90 + pulse * 60), 168, 85, 247);
                        pgb.SurroundColors = new[] { Color.FromArgb(0, 5, 7, 11) };
                        g.FillPath(pgb, haloPath);
                    }
                }
                using (var vf = new SolidBrush(Color.FromArgb(250, 12, 15, 22)))
                    g.FillEllipse(vf, cx - 170, cy - 170, 340, 340);

                float ang = (f * 4.5f) % 360;
                using (var sheenPen = new Pen(Color.FromArgb(70, 255, 255, 255), 1.5f))
                {
                    for (int r = 55; r < 165; r += 12)
                    {
                        using (var gp = new Pen(Color.FromArgb(28, 255, 255, 255), 1f))
                            g.DrawEllipse(gp, cx - r, cy - r, r * 2, r * 2);
                        g.DrawArc(sheenPen, cx - r, cy - r, r * 2, r * 2, ang, 40);
                        g.DrawArc(sheenPen, cx - r, cy - r, r * 2, r * 2, (ang + 180) % 360, 40);
                    }
                }
                using (var lbl = new LinearGradientBrush(new PointF(cx - 46, cy - 46), new PointF(cx + 46, cy + 46), OrchidNeon, RoseCrimson))
                    g.FillEllipse(lbl, cx - 46, cy - 46, 92, 92);
                g.FillEllipse(new SolidBrush(ObsidianVoid), cx - 10, cy - 10, 20, 20);
            }
            else if (mode == 1)
            {
                // 2) FULL SCREEN ART (Breathing + Parallax)
                float scale = 1.0f + 0.045f * (float)Math.Sin(t * 3.0);
                float px = (float)Math.Sin(t * 2.1) * 10f;
                float py = (float)Math.Cos(t * 1.5) * 8f;
                float sz = 290 * scale;
                DrawSpecularCard(g, cx - sz / 2 + px, cy - sz / 2 + py, sz, sz, 36f, Color.FromArgb(255, 26, 22, 68), CyanNeon, 1.5f);
                g.DrawString("MI", FontMonogram, wBrush, cx - 54 + px, cy - 48 + py);
            }
            else if (mode == 2)
            {
                // 3) PARTICLES MODE (18 Orbiting Constellation Nodes)
                using (var ring = new Pen(Color.FromArgb(50, 0, 240, 255), 1.2f))
                    g.DrawEllipse(ring, cx - 165, cy - 165, 330, 330);
                Color[] pal = { CyanNeon, OrchidNeon, EmeraldNeon, AmberTopaz };
                for (int i = 0; i < 18; i++)
                {
                    double orbit = t * (1.2 + (i % 3) * 0.4) + i * (Math.PI * 2.0 / 18.0);
                    double rad = 75 + 85 * (0.5 + 0.5 * Math.Sin(t * 2.2 + i));
                    float pxx = cx + (float)(Math.Cos(orbit) * rad);
                    float pyy = cy + (float)(Math.Sin(orbit) * rad);
                    float pSz = 10 + (i % 4) * 5;
                    using (var pb = new SolidBrush(Color.FromArgb(200, pal[i % pal.Length])))
                        g.FillEllipse(pb, pxx - pSz / 2, pyy - pSz / 2, pSz, pSz);
                }
                g.DrawString("MI", FontHero, wBrush, cx - 16, cy - 14);
            }
            else
            {
                // 4) GLOWING BEATS (16-Band Spectrum + Pulse Ring)
                float bScale = 0.94f + 0.14f * Math.Abs((float)Math.Sin(t * 6.5));
                float rSz = 310 * bScale;
                using (var bPen = new Pen(CyanNeon, 2.2f))
                using (var bFill = new SolidBrush(Color.FromArgb(30, 168, 85, 247)))
                {
                    g.FillEllipse(bFill, cx - rSz / 2, cy - rSz / 2, rSz, rSz);
                    g.DrawEllipse(bPen, cx - rSz / 2, cy - rSz / 2, rSz, rSz);
                }
                for (int b = 0; b < 16; b++)
                {
                    float wave = Math.Abs((float)Math.Sin(t * 7.2 + b * 0.45)) * 0.7f + Math.Abs((float)Math.Cos(t * 4.5 - b * 0.3)) * 0.3f;
                    float bh = 28 + wave * 155;
                    float bx = (cx - 128) + b * 16;
                    using (var br = new LinearGradientBrush(new PointF(bx, cy - 100), new PointF(bx, cy + 100), CyanNeon, OrchidNeon))
                        g.FillRectangle(br, bx, cy - bh / 2, 10, bh);
                }
            }

            // Right Side: Track Info, Transport & 4-Mode Selector Pills
            g.DrawString("MUSIC PLAYER 10/10  •  PAUSE-ON-HIDE GPU LAW ACTIVE", FontMicro, cBrush, 630, 196);
            g.DrawString("Midnight City", FontHero, wBrush, 630, 226);
            g.DrawString("M83  •  Hurry Up, We're Dreaming", FontHeader, mBrush, 630, 264);

            float progW = 460;
            float frac = 0.38f + (f / 360.0f) * 0.15f;
            DrawSpecularCard(g, 630, 322, progW, 6, 3f, Color.FromArgb(60, 255, 255, 255));
            using (var pb = new LinearGradientBrush(new PointF(630, 0), new PointF(630 + progW, 0), CyanNeon, OrchidNeon))
                g.FillRectangle(pb, 630, 322, progW * frac, 6);
            g.FillEllipse(wBrush, 630 + progW * frac - 6, 319, 12, 12);

            g.DrawString("⏮", FontHero, wBrush, 760, 366);
            g.FillEllipse(cBrush, 825, 357, 56, 56);
            g.DrawString("⏸", FontHeader, new SolidBrush(Color.Black), 844, 374);
            g.DrawString("⏭", FontHero, wBrush, 905, 366);

            string[] modeNames = { "Vinyl", "Full Art", "Particles", "Glowing Beats" };
            for (int mi = 0; mi < 4; mi++)
            {
                bool activeM = (mi == mode);
                DrawSpecularCard(g, 630 + mi * 116, 444, 106, 32, 12f,
                    activeM ? Color.FromArgb(45, 0, 240, 255) : Surface2,
                    activeM ? CyanNeon : OrchidNeon, activeM ? 1.4f : 0.7f);
                g.DrawString(modeNames[mi], FontMicro, activeM ? cBrush : wBrush, 646 + mi * 116, 454);
            }
        }
    }

    // SCENE 5: Squish BSP Tiling & Flow 9-Band Dictation (Frames 1440-1649 -> local f 0-209)
    private static void DrawScene5_SquishAndFlow(Graphics g, int f, double t)
    {
        DrawSnootPill(g, 430, 36, "Squish BSP Active", "FLOW 9B", true, false, 0, 0);

        int activeWin = (f < 70) ? 0 : (f < 140 ? 1 : 2);
        DrawWin11AppWindow(g, 16, 52, 616, 608, "Visual Studio Code — LaunchMe OS", activeWin == 0,
            new[] { "// Squish Golden-Ratio BSP + Win32 DeferWindowPos", "NotchWindow: Clipboard Cards + Nearby Share + Focus + Size", "MusicPlayer: Vinyl / FullArt / 18 Particles / 16 BeatBars" });

        DrawWin11AppWindow(g, 646, 52, 618, 296, "Windows Terminal — PowerShell", activeWin == 1,
            new[] { "PS E:\\LaunchMe OS> dotnet test --no-build", "Passed! Platform: 145/145 | Core: 261/261 | UI: 143/143", "Aggregate: 549 / 549 Tests Passing (0 Warnings, 0 Errors)" });

        DrawWin11AppWindow(g, 646, 364, 618, 296, "Flow Voice Dictation — Live Filler Removal", activeWin == 2,
            new[] { "9-Band Spectrum -> Clean Keystroke Injection", "\"Ship LaunchMe v3.01 with full 18-language i18n\"" });

        // Live Flow Audio Pill Floating Bottom-Center
        DrawSpecularCard(g, 460, 580, 360, 56, 20f, Surface2, OrchidNeon, 1.3f);
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var wBrush = new SolidBrush(TextPrimary))
        {
            g.DrawString("FLOW MIC LIVE", FontMicro, cBrush, 480, 598);
            for (int b = 0; b < 9; b++)
            {
                float bh = 8f + (float)(Math.Abs(Math.Sin(t * 9.0 + b * 0.7)) * 22f);
                g.FillRectangle(new SolidBrush(b % 2 == 0 ? CyanNeon : OrchidNeon), 580 + b * 11, 622 - bh, 6, bh);
            }
            g.DrawString("9-BAND", FontMicro, wBrush, 700, 598);
        }
    }

    private static void DrawWin11AppWindow(Graphics g, float x, float y, float w, float h, string title, bool focused, string[] lines)
    {
        DrawSpecularCard(g, x, y, w, h, 12f, Color.FromArgb(244, 18, 22, 32), focused ? CyanNeon : (Color?)null, focused ? 2.2f : 0.8f);
        using (var wBrush = new SolidBrush(TextPrimary))
        using (var mBrush = new SolidBrush(TextMuted))
        using (var cBrush = new SolidBrush(CyanNeon))
        {
            g.DrawString(title, FontBody, focused ? wBrush : mBrush, x + 14, y + 10);
            g.DrawString("—     🗖     ✕", FontBody, wBrush, x + w - 82, y + 9);
            using (var sep = new Pen(Color.FromArgb(30, 255, 255, 255), 1f))
                g.DrawLine(sep, x + 1, y + 34, x + w - 1, y + 34);
            for (int i = 0; i < lines.Length; i++)
                g.DrawString(lines[i], FontMono, i == 0 && focused ? cBrush : wBrush, x + 16, y + 54 + (i * 24));
        }
    }

    // SCENE 6: 18 Languages i18n, Phone Leash E2EE & Pause-on-Hide GPU Finale (Frames 1650-1859 -> local f 0-209)
    private static void DrawScene6_LocalesLeashAndFinale(Graphics g, int f, double t)
    {
        using (var wBrush = new SolidBrush(TextPrimary))
        using (var cBrush = new SolidBrush(CyanNeon))
        using (var eBrush = new SolidBrush(EmeraldNeon))
        using (var oBrush = new SolidBrush(OrchidNeon))
        using (var mBrush = new SolidBrush(TextMuted))
        {
            if (f < 125)
            {
                DrawSnootPill(g, 430, 36, "18 Cultures • Leash E2EE", "AES-GCM", true, false, 0, 0);

                // Left Card: 18 Native Languages Live Switcher
                DrawSpecularCard(g, 80, 96, 540, 480, 24f, Surface2, CyanNeon, 1.1f);
                g.DrawString("18-LANGUAGE LOCALIZATION  •  100% NATIVE TRANSLATIONS", FontMicro, cBrush, 108, 124);
                string[,] locales = {
                    { "en-US", "LaunchMe • Launcher • Clipboard" },
                    { "fr-FR", "Lanceur • Presse-papiers • Concentration" },
                    { "de-DE", "Starter • Zwischenablage • Fokusmodus" },
                    { "ja-JP", "ランチャー • クリップボード • 集中モード" },
                    { "zh-CN", "启动台 • 剪贴板 • 专注模式" },
                    { "ar-SA (RTL)", "المُشغّل • الحافظة • وضع التركيز" },
                    { "hi-IN", "लॉन्चर • क्लिपबोर्ड • फ़ोकस मोड" },
                    { "ko-KR", "런처 • 클립보드 • 집중 모드" }
                };
                int hi = (f / 14) % 8;
                for (int i = 0; i < 8; i++)
                {
                    float ly = 160 + i * 48;
                    DrawSpecularCard(g, 104, ly, 492, 40, 10f,
                        i == hi ? Color.FromArgb(40, 0, 240, 255) : Color.FromArgb(20, 255, 255, 255),
                        i == hi ? CyanNeon : (Color?)null, 0.8f);
                    g.DrawString(locales[i, 0], FontMicro, i == hi ? cBrush : oBrush, 120, ly + 13);
                    g.DrawString(locales[i, 1], FontBody, wBrush, 224, ly + 12);
                }

                // Right Card: Phone Leash + --capture-showcase DPI Capture
                DrawSpecularCard(g, 650, 96, 550, 480, 24f, Surface2, EmeraldNeon, 1.1f);
                g.DrawString("PHONE LEASH E2EE & SHOWCASE DPI CAPTURE", FontMicro, eBrush, 678, 124);
                DrawSpecularCard(g, 678, 160, 494, 150, 16f, Color.FromArgb(210, 12, 16, 26), EmeraldNeon, 0.9f);
                g.DrawString("PAIRING PIN: 482 - 910   •   📱 Pixel 9 Pro Paired", FontHeader, wBrush, 698, 184);
                g.DrawString("AES-GCM 96-bit Nonce • Zero Cloud Telemetry", FontBody, eBrush, 698, 224);
                g.DrawString("PBKDF2 100k Iterations • Local UDP Relay Active", FontBody, mBrush, 698, 252);

                DrawSpecularCard(g, 678, 330, 494, 210, 16f, Color.FromArgb(210, 12, 16, 26), CyanNeon, 0.9f);
                g.DrawString("--capture-showcase DPI VERIFICATION", FontMicro, cBrush, 698, 352);
                g.DrawString("✓ RenderTargetBitmap Surface Capture (100% / 150% / 200%)", FontBody, wBrush, 698, 390);
                g.DrawString("✓ WDA_EXCLUDEFROMCAPTURE Enabled on Snoot HUD", FontBody, wBrush, 698, 420);
                g.DrawString("✓ Pause-on-Hide GPU Law: 0.0% Idle GPU When Hidden", FontBody, eBrush, 698, 450);
            }
            else
            {
                // Finale Card (Clean, Zero Taskbar behind it)
                float cw = 720, ch = 280;
                float cx = (W - cw) / 2f, cy = (H - ch) / 2f;
                DrawSpecularCard(g, cx, cy, cw, ch, 28f, Surface1, CyanNeon, 1.3f);
                g.DrawString("LAUNCHME OS  •  v3.01-WIN-LUXURY", FontHero, wBrush, cx + 128, cy + 48);
                g.DrawString("WINDOWS 11 WORKSPACE SHELL  •  PAUSE-ON-HIDE (0.0% IDLE GPU)", FontMicro, cBrush, cx + 130, cy + 94);
                g.DrawString("✓ 549 / 549 Tests Passing   ✓ 0 Warnings   ✓ 18 Locales   ✓ 12 Clocks", FontSub, eBrush, cx + 112, cy + 144);
                g.DrawString("Snoot HUD 10/10  •  Music Player 10/10  •  Squish  •  Blobbo  •  Leash", FontHeader, mBrush, cx + 76, cy + 196);
            }
        }
    }
}
"@

Write-Host "Compiling C# 30 FPS v3.01 Tour Renderer..." -ForegroundColor Cyan
Add-Type -TypeDefinition $csharpRenderer -ReferencedAssemblies System.Drawing

Write-Host "Rendering 1,860 1280x720 PNG frames @ 30.0 FPS..." -ForegroundColor Cyan
[LaunchMeV3TourRenderer]::RenderAll($frameDumpDir)

$ffmpegExe = "C:\Users\Redwan Ahmmed\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg.Essentials_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0.1-essentials_build\bin\ffmpeg.exe"
$finalMp4 = "$outDir\launchme_os_complete_tour_30fps.mp4"
$finalWebm = "$outDir\launchme_os_complete_tour_30fps.webm"

Write-Host "Encoding MP4 (H.264, 30fps) & WebM (VP9, 30fps)..." -ForegroundColor Cyan
& $ffmpegExe -y -framerate 30 -i "$frameDumpDir\frame_%05d.png" -c:v libx264 -pix_fmt yuv420p -preset fast -crf 19 -r 30 "$finalMp4"
& $ffmpegExe -y -i "$finalMp4" -c:v libvpx-vp9 -b:v 2400k -r 30 "$finalWebm"

Copy-Item "$finalMp4" "$distVideoDir\" -Force
Copy-Item "$finalWebm" "$distVideoDir\" -Force

Remove-Item $frameDumpDir -Recurse -Force

Write-Host "=== Complete 30 FPS v3.01 OS Tour Rendered! ===" -ForegroundColor Green
Get-Item "$finalMp4", "$finalWebm" | Select-Object Name, @{Name="Size (MB)"; Expression={[Math]::Round($_.Length / 1MB, 2)}}
