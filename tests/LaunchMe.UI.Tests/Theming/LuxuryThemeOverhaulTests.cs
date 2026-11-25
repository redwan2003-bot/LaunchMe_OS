using System.Numerics;
using FluentAssertions;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Contracts.Tiling;
using LaunchMe.UI.Theming;
using LaunchMe.UI.Tiling;
using LaunchMe.UI.Views;
using Microsoft.Graphics.Canvas.Effects;
using Xunit;

namespace LaunchMe.UI.Tests.Theming;

public sealed class LuxuryThemeOverhaulTests
{
    [Fact]
    public void SquircleGeometryBuilder_UsesL15286Factor_AndEightCubicBeziers()
    {
        var def = SquircleGeometryBuilder.BuildPathDefinition(width: 200f, height: 200f, cornerRadius: 24f);

        def.EffectiveRadius.Should().BeApproximately(24f, 0.001f);
        def.CornerExtentL.Should().BeApproximately(1.5286f * 24f, 0.001f);
        def.Quadrants.Should().HaveCount(4);
        def.CubicBezierCount.Should().Be(8);

        // Verify G2 zero-curvature collinear join on top-right entry edge (y == 0)
        var topRight = def.Quadrants[0];
        topRight.EntryPoint.Y.Should().Be(0f);
        topRight.FirstBezier.Control1.Y.Should().Be(0f);
        topRight.FirstBezier.Control2.Y.Should().Be(0f);

        // Verify 45-degree apex tangent symmetry
        Vector2 incomingTangent = topRight.FirstBezier.EndPoint - topRight.FirstBezier.Control2;
        Vector2 outgoingTangent = topRight.SecondBezier.Control1 - topRight.FirstBezier.EndPoint;
        incomingTangent.X.Should().BeApproximately(outgoingTangent.X, 0.001f);
        incomingTangent.Y.Should().BeApproximately(outgoingTangent.Y, 0.001f);
    }

    [Fact]
    public void SquircleGeometryBuilder_ClampsRadiusForSmallRects_AndBuildsSvgPath()
    {
        var def = SquircleGeometryBuilder.BuildPathDefinition(width: 64f, height: 64f, cornerRadius: 40f);

        // Max effective radius = 64 / (2 * 1.5286)
        float expectedMaxR = 64f / (2f * 1.5286f);
        def.EffectiveRadius.Should().BeApproximately(expectedMaxR, 0.001f);
        def.CornerExtentL.Should().BeApproximately(32f, 0.01f);

        string svg = SquircleGeometryBuilder.BuildSvgPathData(64f, 64f, 18f);
        svg.Should().StartWith("M ");
        svg.Should().Contain(" C ");
        svg.Should().EndWith(" Z");

        // Headless safety when Compositor is null
        SquircleGeometryBuilder.TryCreateCompositionPathGeometry(null, 64f, 64f, 18f).Should().BeNull();
    }

    [Fact]
    public void LiquidGlassCompositor_ExposesEditorialPipelineParameters_AndDegradesSafelyHeadless()
    {
        using var compositor = new LiquidGlassCompositor();

        compositor.BlurAmount.Should().Be(42.0f);
        compositor.Saturation.Should().Be(1.80f);
        compositor.GrainOpacity.Should().Be(0.025f);
        LiquidGlassCompositor.ObsidianTintColor.R.Should().Be(0x08);
        LiquidGlassCompositor.ObsidianTintColor.G.Should().Be(0x0A);
        LiquidGlassCompositor.ObsidianTintColor.B.Should().Be(0x0F);

        var matrix = LiquidGlassCompositor.BuildObsidianColorMatrix();
        matrix.M51.Should().BeApproximately((8f / 255f) * 0.65f, 0.0001f);
        matrix.M52.Should().BeApproximately((10f / 255f) * 0.65f, 0.0001f);
        matrix.M53.Should().BeApproximately((15f / 255f) * 0.62f, 0.0001f);

        // Headless safety without WinRT Windows App SDK bootstrap or Compositor
        var effectGraph = compositor.TryBuildEffectGraph(includeGrain: true);
        if (effectGraph is CompositeEffect composite)
        {
            composite.Sources.Should().HaveCount(2);
        }

        compositor.TryCreateEffectBrush(null).Should().BeNull();
        compositor.TryAttachToElement(null).Should().BeFalse();
        compositor.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ThemeVisualState_AndTintService_ResolveObsidianAndElectricCyanTokens()
    {
        var tintService = new LiquidGlassTintService();
        var state = tintService.ResolveVisualState(new ThemeSettings
        {
            Style = "LiquidGlass",
            Mode = "Tinted",
            TintHex = "#080A0F",
            Intensity = 0.85,
            TitleColor = "Auto"
        });

        state.IsLiquidGlass.Should().BeTrue();
        state.IsTinted.Should().BeTrue();
        state.TintColor.R.Should().Be(0x08);
        state.TintColor.G.Should().Be(0x0A);
        state.TintColor.B.Should().Be(0x0F);
        state.EffectiveTitleHex.Should().Be("#F9FAFB");
        state.BlurIntensity.Should().BeGreaterThan(40.0);

        var cyan = LiquidGlassTintService.ParseHexColor("#00F0FF", default);
        cyan.R.Should().Be(0x00);
        cyan.G.Should().Be(0xF0);
        cyan.B.Should().Be(0xFF);
    }

    [Fact]
    public void SnootSpringController_AndDockSpringHelper_HaveExactPhysicsAndHeadlessSafety()
    {
        SnootSpringController.ExpandDampingRatio.Should().Be(0.82f);
        SnootSpringController.ExpandPeriodMs.Should().Be(220);
        SnootSpringController.CollapseDampingRatio.Should().Be(0.88f);
        SnootSpringController.CollapsePeriodMs.Should().Be(180);

        using var controller = new SnootSpringController();
        controller.IsAttached.Should().BeFalse();

        controller.AnimateExpand().Should().BeFalse();
        controller.IsLastExpanded.Should().BeTrue();

        controller.AnimateCollapse().Should().BeFalse();
        controller.IsLastExpanded.Should().BeFalse();

        // Pause-on-Hide stop & detach safety in headless mode
        var stopAct = () =>
        {
            controller.StopAnimation();
            controller.Detach();
        };
        stopAct.Should().NotThrow();
        controller.IsAttached.Should().BeFalse();

        // Dock hover spring helper safe on null UIElement
        var act = () =>
        {
            DockWindow.ApplyDockItemHoverSpring(null, isHovered: true);
            DockWindow.ApplyDockItemHoverSpring(null, isHovered: false);
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void FocusBorderService_AndTiledDropPreviewService_HaveNeonDefaults_AndEnforcePauseOnHide()
    {
        var focus = new FocusBorderService();
        var drop = new TiledDropPreviewService();

        // Neon glow configuration defaults
        focus.NeonColorHex.Should().Be("#00F0FF");
        focus.BlurRadiusPx.Should().Be(22.0);
        focus.CornerRadiusPx.Should().Be(12.0);

        drop.NeonColorHex.Should().Be("#00F0FF");
        drop.BlurRadiusPx.Should().Be(22.0);
        drop.CornerRadiusPx.Should().Be(12.0);

        // Pause-on-Hide when initially hidden -> false / zero
        focus.IsVisible.Should().BeFalse();
        focus.IsGlowActive.Should().BeFalse();
        focus.EffectiveBlurRadiusPx.Should().Be(0.0);

        drop.IsVisible.Should().BeFalse();
        drop.IsAnimationActive.Should().BeFalse();
        drop.EffectiveBlurRadiusPx.Should().Be(0.0);

        // Active when shown
        var bounds = new WindowRect(10, 20, 1280, 720);
        focus.AttachToWindow(new nint(42), bounds);
        drop.ShowPreview(bounds);

        focus.IsGlowActive.Should().BeTrue();
        focus.EffectiveBlurRadiusPx.Should().Be(22.0);
        drop.IsAnimationActive.Should().BeTrue();
        drop.EffectiveBlurRadiusPx.Should().Be(22.0);

        // Pause-on-Hide enforced on Hide -> false / zero
        focus.HideBorder();
        drop.HidePreview();

        focus.IsVisible.Should().BeFalse();
        focus.IsGlowActive.Should().BeFalse();
        focus.EffectiveBlurRadiusPx.Should().Be(0.0);

        drop.IsVisible.Should().BeFalse();
        drop.IsAnimationActive.Should().BeFalse();
        drop.EffectiveBlurRadiusPx.Should().Be(0.0);
    }
}
