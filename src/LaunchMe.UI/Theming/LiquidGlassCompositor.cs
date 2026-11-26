using System.Numerics;
using Windows.Graphics.Effects;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace LaunchMe.UI.Theming;

/// <summary>
/// Builds and attaches the multi-stage Liquid Glass Win2D/Composition backdrop pipeline:
/// GaussianBlurEffect (42f) -> SaturationEffect (1.80f) -> ColorMatrixEffect (Obsidian #080A0F) -> TurbulenceEffect (2.5% grain).
/// Gracefully degrades when Dispatcher or Compositor is unavailable (e.g., headless unit tests).
/// </summary>
public sealed class LiquidGlassCompositor : IDisposable
{
    public const float DefaultBlurAmount = 42.0f;
    public const float DefaultSaturation = 1.80f;
    public const float DefaultGrainOpacity = 0.025f;
    public const float DefaultChromaticFringePx = 1.5f;

    public static readonly Color ObsidianTintColor = Color.FromArgb(255, 8, 10, 15); // #080A0F

    private CompositionEffectFactory? _effectFactory;
    private CompositionEffectBrush? _effectBrush;
    private SpriteVisual? _backdropVisual;
    private bool _disposed;

    public float BlurAmount { get; set; } = DefaultBlurAmount;
    public float Saturation { get; set; } = DefaultSaturation;
    public float GrainOpacity { get; set; } = DefaultGrainOpacity;
    public float ChromaticFringePx { get; set; } = DefaultChromaticFringePx;
    public Color SampledRefractionColor { get; private set; } = ObsidianTintColor;

    /// <summary>
    /// True if a live CompositionEffectBrush was created and attached.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Computes the 5x4 color matrix blending backdrop luminance into Pure OLED Obsidian (#080A0F)
    /// or an optional wallpaper-sampled refraction tint.
    /// </summary>
    public static Matrix5x4 BuildObsidianColorMatrix(Color? sampledTint = null)
    {
        Color tint = sampledTint ?? ObsidianTintColor;
        float rOffset = (tint.R / 255f) * 0.65f;
        float gOffset = (tint.G / 255f) * 0.65f;
        float bOffset = (tint.B / 255f) * 0.62f;

        return new Matrix5x4
        {
            M11 = 0.35f, M12 = 0f,    M13 = 0f,    M14 = 0f,
            M21 = 0f,    M22 = 0.35f, M23 = 0f,    M24 = 0f,
            M31 = 0f,    M32 = 0f,    M33 = 0.38f, M34 = 0f,
            M41 = 0f,    M42 = 0f,    M43 = 0f,    M44 = 1f,
            M51 = rOffset,
            M52 = gOffset,
            M53 = bOffset,
            M54 = 0f
        };
    }

    /// <summary>
    /// Blends a sampled wallpaper RGB color into a subtle crystal-refraction obsidian tint
    /// so Liquid Glass carries live desktop hue without washing out white text contrast.
    /// </summary>
    public Color ComputeWallpaperRefractionTint(Color wallpaperSample, float refractionStrength = 0.22f)
    {
        float t = Math.Clamp(refractionStrength, 0f, 0.45f);
        byte r = (byte)Math.Clamp(ObsidianTintColor.R * (1f - t) + wallpaperSample.R * t, 0, 255);
        byte g = (byte)Math.Clamp(ObsidianTintColor.G * (1f - t) + wallpaperSample.G * t, 0, 255);
        byte b = (byte)Math.Clamp(ObsidianTintColor.B * (1f - t) + wallpaperSample.B * t, 0, 255);
        SampledRefractionColor = Color.FromArgb(255, r, g, b);
        return SampledRefractionColor;
    }

    /// <summary>
    /// Safely attempts to build the Win2D effect graph, returning null when WinRT activation
    /// is unavailable in headless unit test environments.
    /// </summary>
    public IGraphicsEffect? TryBuildEffectGraph(bool includeGrain = true)
    {
        try
        {
            return BuildEffectGraph(includeGrain);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds the 4-stage Win2D effect graph description:
    /// GaussianBlurEffect(42f) -> SaturationEffect(1.80f) -> ColorMatrixEffect(#080A0F) -> TurbulenceEffect(2.5% grain).
    /// </summary>
    public IGraphicsEffect BuildEffectGraph(bool includeGrain = true)
    {
        var blur = new GaussianBlurEffect
        {
            Name = "GlassBlur",
            BlurAmount = BlurAmount,
            BorderMode = EffectBorderMode.Hard,
            Optimization = EffectOptimization.Balanced,
            Source = new CompositionEffectSourceParameter("backdrop")
        };

        var saturation = new SaturationEffect
        {
            Name = "GlassSaturation",
            Saturation = Saturation,
            Source = blur
        };

        var obsidianTint = new ColorMatrixEffect
        {
            Name = "GlassObsidianMatrix",
            Source = saturation,
            ColorMatrix = BuildObsidianColorMatrix()
        };

        if (!includeGrain || GrainOpacity <= 0f)
        {
            return obsidianTint;
        }

        var turbulence = new TurbulenceEffect
        {
            Name = "GlassTurbulence",
            Frequency = new Vector2(0.85f, 0.85f),
            Size = new Vector2(512f, 512f),
            Octaves = 2
        };

        var scaledGrain = new ColorMatrixEffect
        {
            Name = "GlassGrainOpacity",
            Source = turbulence,
            ColorMatrix = new Matrix5x4
            {
                M11 = 1f, M12 = 0f, M13 = 0f, M14 = 0f,
                M21 = 0f, M22 = 1f, M23 = 0f, M24 = 0f,
                M31 = 0f, M32 = 0f, M33 = 1f, M34 = 0f,
                M41 = 0f, M42 = 0f, M43 = 0f, M44 = Math.Clamp(GrainOpacity, 0f, 1f)
            }
        };

        var composite = new CompositeEffect
        {
            Name = "GlassComposite",
            Mode = CanvasComposite.SourceOver
        };
        composite.Sources.Add(obsidianTint);
        composite.Sources.Add(scaledGrain);
        return composite;
    }

    /// <summary>
    /// Attempts to create a <see cref="CompositionEffectBrush"/> using the provided Compositor.
    /// Returns null safely when Compositor is unavailable or DComp rejects an effect stage.
    /// </summary>
    public CompositionEffectBrush? TryCreateEffectBrush(Compositor? compositor)
    {
        if (_disposed || compositor == null)
        {
            return null;
        }

        // First try full 4-stage pipeline with TurbulenceEffect grain; fall back to 3-stage if DComp rejects TurbulenceEffect.
        foreach (bool withGrain in new[] { true, false })
        {
            try
            {
                var effect = BuildEffectGraph(withGrain);
                _effectFactory?.Dispose();
                _effectFactory = compositor.CreateEffectFactory(effect);

                var brush = _effectFactory.CreateBrush();
                brush.SetSourceParameter("backdrop", compositor.CreateBackdropBrush());

                _effectBrush?.Dispose();
                _effectBrush = brush;
                IsActive = true;
                return brush;
            }
            catch
            {
                // Try fallback stage or headless safe-exit
            }
        }

        IsActive = false;
        return null;
    }

    /// <summary>
    /// Safely attaches the Liquid Glass CompositionEffectBrush as a child SpriteVisual on the target UIElement.
    /// Returns true if attached, false if running headless or without a Compositor.
    /// </summary>
    public bool TryAttachToElement(UIElement? target, float width = 0f, float height = 0f)
    {
        if (_disposed || target == null)
        {
            return false;
        }

        try
        {
            var hostVisual = ElementCompositionPreview.GetElementVisual(target);
            if (hostVisual == null)
            {
                return false;
            }

            var compositor = hostVisual.Compositor;
            var brush = TryCreateEffectBrush(compositor);
            if (brush == null)
            {
                return false;
            }

            _backdropVisual = compositor.CreateSpriteVisual();
            _backdropVisual.Brush = brush;
            _backdropVisual.RelativeSizeAdjustment = Vector2.One;
            if (width > 0f && height > 0f)
            {
                _backdropVisual.Size = new Vector2(width, height);
            }

            ElementCompositionPreview.SetElementChildVisual(target, _backdropVisual);
            return true;
        }
        catch
        {
            IsActive = false;
            return false;
        }
    }

    /// <summary>
    /// Detaches the composition visual cleanly (Pause-on-Hide GPU Law).
    /// </summary>
    public void Detach(UIElement? target = null)
    {
        IsActive = false;
        if (target != null)
        {
            try
            {
                ElementCompositionPreview.SetElementChildVisual(target, null);
            }
            catch
            {
            }
        }

        _backdropVisual?.Dispose();
        _backdropVisual = null;
        _effectBrush?.Dispose();
        _effectBrush = null;
        _effectFactory?.Dispose();
        _effectFactory = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Detach();
    }
}
