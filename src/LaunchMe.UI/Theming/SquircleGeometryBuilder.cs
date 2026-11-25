using System.Globalization;
using System.Numerics;
using System.Text;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;

namespace LaunchMe.UI.Theming;

/// <summary>
/// Builds G2-continuous superellipse (squircle) geometries using 2 cubic Bezier curves per quadrant
/// with extended corner footprint L = 1.5286f * r for Apple/Editorial-grade continuous curvature.
/// </summary>
public static class SquircleGeometryBuilder
{
    /// <summary>
    /// Continuous corner extent multiplier (L = 1.5286f * r).
    /// </summary>
    public const float CornerExtentFactor = 1.5286f;

    /// <summary>
    /// 45-degree apex offset along each axis (1 - 1/sqrt(2)) * r, ensuring G2 zero-curvature
    /// transition into straight edges and C1/G2 symmetry across the diagonal apex.
    /// </summary>
    public const float ApexOffsetFactor = 0.29289322f;

    /// <summary>
    /// Computes the effective corner radius and L = 1.5286f * r footprint clamped to fit the bounds.
    /// </summary>
    public static (float EffectiveRadius, float CornerExtentL) ComputeEffectiveMetrics(
        float width,
        float height,
        float cornerRadius)
    {
        if (width <= 0f || height <= 0f || cornerRadius <= 0f)
        {
            return (0f, 0f);
        }

        float maxRadius = MathF.Min(width, height) / (2f * CornerExtentFactor);
        float effectiveR = MathF.Min(cornerRadius, maxRadius);
        float extentL = CornerExtentFactor * effectiveR;
        return (effectiveR, extentL);
    }

    /// <summary>
    /// Builds the complete 4-quadrant (8 cubic Bezier) G2-continuous squircle path definition.
    /// </summary>
    public static SquirclePathDefinition BuildPathDefinition(float width, float height, float cornerRadius)
    {
        if (width <= 0f || height <= 0f)
        {
            return new SquirclePathDefinition(0f, 0f, 0f, 0f, []);
        }

        var (r, l) = ComputeEffectiveMetrics(width, height, cornerRadius);
        if (r <= 0.0001f)
        {
            return new SquirclePathDefinition(width, height, 0f, 0f, []);
        }

        float d = ApexOffsetFactor * r;
        float twoD = 2f * d;
        float leadOffset = (l - twoD) * 0.5f;

        // 4 quadrants clockwise: TopRight, BottomRight, BottomLeft, TopLeft.
        // Each quadrant consists of 2 cubic Bezier segments meeting at the 45-degree corner apex.
        var quadrants = new SquircleQuadrant[4];

        // 1. Top-Right: (W - L, 0) -> Apex (W - d, d) -> (W, L)
        quadrants[0] = new SquircleQuadrant(
            EntryPoint: new Vector2(width - l, 0f),
            FirstBezier: new CubicBezierSegment(
                new Vector2(width - l + leadOffset, 0f),
                new Vector2(width - twoD, 0f),
                new Vector2(width - d, d)),
            SecondBezier: new CubicBezierSegment(
                new Vector2(width, twoD),
                new Vector2(width, l - leadOffset),
                new Vector2(width, l)));

        // 2. Bottom-Right: (W, H - L) -> Apex (W - d, H - d) -> (W - L, H)
        quadrants[1] = new SquircleQuadrant(
            EntryPoint: new Vector2(width, height - l),
            FirstBezier: new CubicBezierSegment(
                new Vector2(width, height - l + leadOffset),
                new Vector2(width, height - twoD),
                new Vector2(width - d, height - d)),
            SecondBezier: new CubicBezierSegment(
                new Vector2(width - twoD, height),
                new Vector2(width - l + leadOffset, height),
                new Vector2(width - l, height)));

        // 3. Bottom-Left: (L, H) -> Apex (d, H - d) -> (0, H - L)
        quadrants[2] = new SquircleQuadrant(
            EntryPoint: new Vector2(l, height),
            FirstBezier: new CubicBezierSegment(
                new Vector2(l - leadOffset, height),
                new Vector2(twoD, height),
                new Vector2(d, height - d)),
            SecondBezier: new CubicBezierSegment(
                new Vector2(0f, height - twoD),
                new Vector2(0f, height - l + leadOffset),
                new Vector2(0f, height - l)));

        // 4. Top-Left: (0, L) -> Apex (d, d) -> (L, 0)
        quadrants[3] = new SquircleQuadrant(
            EntryPoint: new Vector2(0f, l),
            FirstBezier: new CubicBezierSegment(
                new Vector2(0f, l - leadOffset),
                new Vector2(0f, twoD),
                new Vector2(d, d)),
            SecondBezier: new CubicBezierSegment(
                new Vector2(twoD, 0f),
                new Vector2(l - leadOffset, 0f),
                new Vector2(l, 0f)));

        return new SquirclePathDefinition(width, height, r, l, quadrants);
    }

    /// <summary>
    /// Builds a Win2D <see cref="CanvasGeometry"/> representing the G2 squircle.
    /// </summary>
    public static CanvasGeometry CreateCanvasGeometry(
        ICanvasResourceCreator resourceCreator,
        float width,
        float height,
        float cornerRadius)
    {
        ArgumentNullException.ThrowIfNull(resourceCreator);

        var def = BuildPathDefinition(width, height, cornerRadius);
        using var builder = new CanvasPathBuilder(resourceCreator);

        if (def.Quadrants.Count == 0)
        {
            builder.BeginFigure(0f, 0f);
            builder.AddLine(width, 0f);
            builder.AddLine(width, height);
            builder.AddLine(0f, height);
            builder.EndFigure(CanvasFigureLoop.Closed);
            return CanvasGeometry.CreatePath(builder);
        }

        builder.BeginFigure(def.Quadrants[0].EntryPoint);

        for (int i = 0; i < def.Quadrants.Count; i++)
        {
            var q = def.Quadrants[i];
            if (i > 0)
            {
                builder.AddLine(q.EntryPoint);
            }

            builder.AddCubicBezier(q.FirstBezier.Control1, q.FirstBezier.Control2, q.FirstBezier.EndPoint);
            builder.AddCubicBezier(q.SecondBezier.Control1, q.SecondBezier.Control2, q.SecondBezier.EndPoint);
        }

        builder.EndFigure(CanvasFigureLoop.Closed);
        return CanvasGeometry.CreatePath(builder);
    }

    /// <summary>
    /// Safely creates a WinUI CompositionPathGeometry from a shared Win2D device, returning null
    /// when running in headless unit tests or environments without a compositor.
    /// </summary>
    public static CompositionPathGeometry? TryCreateCompositionPathGeometry(
        Compositor? compositor,
        float width,
        float height,
        float cornerRadius)
    {
        if (compositor == null || width <= 0f || height <= 0f)
        {
            return null;
        }

        try
        {
            var device = CanvasDevice.GetSharedDevice();
            using var canvasGeo = CreateCanvasGeometry(device, width, height, cornerRadius);
            var compPath = new CompositionPath(canvasGeo);
            return compositor.CreatePathGeometry(compPath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates an SVG/XAML path mini-language string for the G2 squircle.
    /// </summary>
    public static string BuildSvgPathData(float width, float height, float cornerRadius)
    {
        var def = BuildPathDefinition(width, height, cornerRadius);
        return def.ToSvgPathData();
    }
}

public readonly record struct CubicBezierSegment(Vector2 Control1, Vector2 Control2, Vector2 EndPoint);

public readonly record struct SquircleQuadrant(
    Vector2 EntryPoint,
    CubicBezierSegment FirstBezier,
    CubicBezierSegment SecondBezier);

public sealed class SquirclePathDefinition
{
    public float Width { get; }
    public float Height { get; }
    public float EffectiveRadius { get; }
    public float CornerExtentL { get; }
    public IReadOnlyList<SquircleQuadrant> Quadrants { get; }

    public SquirclePathDefinition(
        float width,
        float height,
        float effectiveRadius,
        float cornerExtentL,
        IReadOnlyList<SquircleQuadrant> quadrants)
    {
        Width = width;
        Height = height;
        EffectiveRadius = effectiveRadius;
        CornerExtentL = cornerExtentL;
        Quadrants = quadrants;
    }

    public int CubicBezierCount => Quadrants.Count * 2;

    public string ToSvgPathData()
    {
        if (Width <= 0f || Height <= 0f)
        {
            return string.Empty;
        }

        if (Quadrants.Count == 0)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"M 0,0 L {Width:0.###},0 L {Width:0.###},{Height:0.###} L 0,{Height:0.###} Z");
        }

        var sb = new StringBuilder();
        var first = Quadrants[0];
        sb.Append(CultureInfo.InvariantCulture, $"M {first.EntryPoint.X:0.###},{first.EntryPoint.Y:0.###}");

        for (int i = 0; i < Quadrants.Count; i++)
        {
            var q = Quadrants[i];
            if (i > 0)
            {
                sb.Append(CultureInfo.InvariantCulture, $" L {q.EntryPoint.X:0.###},{q.EntryPoint.Y:0.###}");
            }

            AppendCubic(sb, q.FirstBezier);
            AppendCubic(sb, q.SecondBezier);
        }

        sb.Append(" Z");
        return sb.ToString();
    }

    private static void AppendCubic(StringBuilder sb, CubicBezierSegment seg)
    {
        sb.Append(CultureInfo.InvariantCulture,
            $" C {seg.Control1.X:0.###},{seg.Control1.Y:0.###} {seg.Control2.X:0.###},{seg.Control2.Y:0.###} {seg.EndPoint.X:0.###},{seg.EndPoint.Y:0.###}");
    }
}
