using Windows.UI;
using FluentAssertions;
using LaunchMe.Contracts.Spaces;
using LaunchMe.UI.Theming;
using Xunit;

namespace LaunchMe.UI.Tests.Theming;

public sealed class LiquidGlassTintServiceTests
{
    private readonly LiquidGlassTintService _service = new();

    [Fact]
    public void ParseHexColor_ParsesStandard6DigitHex_Correctly()
    {
        // Arrange
        string hex = "#1E40AF";

        // Act
        Color color = LiquidGlassTintService.ParseHexColor(hex, Color.FromArgb(255, 0, 0, 0));

        // Assert
        color.A.Should().Be(255);
        color.R.Should().Be(0x1E);
        color.G.Should().Be(0x40);
        color.B.Should().Be(0xAF);
    }

    [Fact]
    public void ParseHexColor_Parses3DigitHex_Correctly()
    {
        // Arrange
        string hex = "#F80";

        // Act
        Color color = LiquidGlassTintService.ParseHexColor(hex, Color.FromArgb(255, 0, 0, 0));

        // Assert
        color.A.Should().Be(255);
        color.R.Should().Be(0xFF);
        color.G.Should().Be(0x88);
        color.B.Should().Be(0x00);
    }

    [Fact]
    public void ParseHexColor_Parses8DigitHexWithAlpha_Correctly()
    {
        // Arrange
        string hex = "#80123456";

        // Act
        Color color = LiquidGlassTintService.ParseHexColor(hex, Color.FromArgb(255, 0, 0, 0));

        // Assert
        color.A.Should().Be(0x80);
        color.R.Should().Be(0x12);
        color.G.Should().Be(0x34);
        color.B.Should().Be(0x56);
    }

    [Fact]
    public void ParseHexColor_InvalidHex_ReturnsFallbackColor()
    {
        // Arrange
        Color fallback = Color.FromArgb(255, 12, 34, 56);

        // Act
        Color color = LiquidGlassTintService.ParseHexColor("not-a-color", fallback);

        // Assert
        color.Should().Be(fallback);
    }

    [Fact]
    public void CalculateLuminance_White_ReturnsOne()
    {
        // Arrange
        Color white = Color.FromArgb(255, 255, 255, 255);

        // Act
        double lum = LiquidGlassTintService.CalculateLuminance(white);

        // Assert
        lum.Should().BeApproximately(1.0, 0.01);
    }

    [Fact]
    public void CalculateLuminance_Black_ReturnsZero()
    {
        // Arrange
        Color black = Color.FromArgb(255, 0, 0, 0);

        // Act
        double lum = LiquidGlassTintService.CalculateLuminance(black);

        // Assert
        lum.Should().Be(0.0);
    }

    [Fact]
    public void ResolveVisualState_LiquidGlassTinted_CalculatesBrushesAndOpacity()
    {
        // Arrange
        var settings = new ThemeSettings
        {
            Style = "LiquidGlass",
            Mode = "Tinted",
            TintHex = "#38BDF8",
            Intensity = 0.8,
            TitleColor = "Auto"
        };

        // Act
        var state = _service.ResolveVisualState(settings);

        // Assert
        state.IsLiquidGlass.Should().BeTrue();
        state.IsTinted.Should().BeTrue();
        state.TintOpacity.Should().Be(0.8);
        state.TintColor.R.Should().Be(0x38);
        state.TintColor.G.Should().Be(0xBD);
        state.TintColor.B.Should().Be(0xF8);
        state.BackdropColor.A.Should().BeGreaterThan(0);
        state.BlurIntensity.Should().BeGreaterThan(20.0);
    }

    [Fact]
    public void ResolveVisualState_FlatMode_SetsBlurToZero()
    {
        // Arrange
        var settings = new ThemeSettings
        {
            Style = "Flat",
            Mode = "Clear",
            Intensity = 0.5,
            TitleColor = "Auto"
        };

        // Act
        var state = _service.ResolveVisualState(settings);

        // Assert
        state.IsLiquidGlass.Should().BeFalse();
        state.BlurIntensity.Should().Be(0.0);
    }

    [Fact]
    public void ResolveVisualState_AutoTitleColor_OnBrightTint_SelectsDarkTitle()
    {
        // Arrange: Bright yellow tint #FFFF00
        var settings = new ThemeSettings
        {
            Style = "LiquidGlass",
            Mode = "Tinted",
            TintHex = "#FFFF00",
            Intensity = 0.9,
            TitleColor = "Auto"
        };

        // Act
        var state = _service.ResolveVisualState(settings);

        // Assert: High luminance background produces dark title text for readability
        state.EffectiveTitleHex.Should().Be("#111827");
    }

    [Fact]
    public void ResolveVisualState_AutoTitleColor_OnDarkTint_SelectsLightTitle()
    {
        // Arrange: Dark navy tint #0F172A
        var settings = new ThemeSettings
        {
            Style = "LiquidGlass",
            Mode = "Tinted",
            TintHex = "#0F172A",
            Intensity = 0.7,
            TitleColor = "Auto"
        };

        // Act
        var state = _service.ResolveVisualState(settings);

        // Assert: Dark background produces light title text
        state.EffectiveTitleHex.Should().Be("#F9FAFB");
    }

    [Fact]
    public void ResolveVisualState_ExplicitTitleColor_OverridesAutoCalculation()
    {
        // Arrange
        var settings = new ThemeSettings
        {
            Style = "LiquidGlass",
            Mode = "Tinted",
            TintHex = "#0F172A",
            Intensity = 0.7,
            TitleColor = "#FF0000"
        };

        // Act
        var state = _service.ResolveVisualState(settings);

        // Assert
        state.EffectiveTitleColor.R.Should().Be(255);
        state.EffectiveTitleColor.G.Should().Be(0);
        state.EffectiveTitleColor.B.Should().Be(0);
    }
}
