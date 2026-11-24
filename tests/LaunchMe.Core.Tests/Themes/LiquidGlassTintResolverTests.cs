using FluentAssertions;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Core.Themes;
using Xunit;

namespace LaunchMe.Core.Tests.Themes;

public sealed class LiquidGlassTintResolverTests
{
    [Fact]
    public void Resolve_LiquidGlass_Tinted_ReturnsTranslucentWithBlur()
    {
        var settings = new ThemeSettings
        {
            Style = "LiquidGlass",
            Mode = "Tinted",
            TintHex = "#1E40AF",
            Intensity = 0.65,
            TitleColor = "Auto"
        };

        var resolution = LiquidGlassTintResolver.Resolve(settings);

        resolution.IsLiquidGlass.Should().BeTrue();
        resolution.IsTinted.Should().BeTrue();
        resolution.IsFlat.Should().BeFalse();
        resolution.BackgroundTint.R.Should().Be(30);
        resolution.BackgroundTint.G.Should().Be(64);
        resolution.BackgroundTint.B.Should().Be(175);
        resolution.BackgroundTint.A.Should().BeGreaterThan(0).And.BeLessThan(255);
        resolution.BlurRadius.Should().BeInRange(20.0, 60.0);
        resolution.RefractionIndex.Should().BeGreaterThan(1.0);
        resolution.BorderHighlight.A.Should().BeGreaterThan(0);
        resolution.TitleColor.Should().NotBe(default);
    }

    [Fact]
    public void Resolve_LiquidGlass_Clear_ReturnsFrostedWhiteSheen()
    {
        var settings = new ThemeSettings
        {
            Style = "LiquidGlass",
            Mode = "Clear",
            TintHex = "#000000",
            Intensity = 0.50,
            TitleColor = "Auto"
        };

        var resolution = LiquidGlassTintResolver.Resolve(settings);

        resolution.IsLiquidGlass.Should().BeTrue();
        resolution.IsTinted.Should().BeFalse();
        resolution.BackgroundTint.R.Should().Be(255);
        resolution.BackgroundTint.G.Should().Be(255);
        resolution.BackgroundTint.B.Should().Be(255);
        resolution.BackgroundTint.A.Should().BeGreaterThan(0).And.BeLessThan(255);
        resolution.BlurRadius.Should().BeInRange(20.0, 60.0);
    }

    [Fact]
    public void Resolve_Flat_Tinted_ReturnsZeroBlur()
    {
        var settings = new ThemeSettings
        {
            Style = "Flat",
            Mode = "Tinted",
            TintHex = "#1E40AF",
            Intensity = 1.0,
            TitleColor = "Auto"
        };

        var resolution = LiquidGlassTintResolver.Resolve(settings);

        resolution.IsLiquidGlass.Should().BeFalse();
        resolution.IsFlat.Should().BeTrue();
        resolution.BlurRadius.Should().Be(0.0);
        resolution.RefractionIndex.Should().Be(1.0);
        resolution.BackgroundTint.A.Should().Be(255); // 1.0 intensity -> opaque
    }

    [Fact]
    public void Resolve_ClampsIntensityCorrectly()
    {
        var negativeSettings = new ThemeSettings { Style = "LiquidGlass", Intensity = -1.0 };
        var overSettings = new ThemeSettings { Style = "LiquidGlass", Intensity = 5.0 };

        var resNegative = LiquidGlassTintResolver.Resolve(negativeSettings);
        var resOver = LiquidGlassTintResolver.Resolve(overSettings);

        resNegative.BlurRadius.Should().Be(20.0); // min blur
        resOver.BlurRadius.Should().Be(60.0);     // max blur
    }
}
