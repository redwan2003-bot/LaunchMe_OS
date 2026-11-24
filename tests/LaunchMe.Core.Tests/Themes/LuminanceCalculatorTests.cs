using FluentAssertions;
using LaunchMe.Core.Themes;
using Xunit;

namespace LaunchMe.Core.Tests.Themes;

public sealed class LuminanceCalculatorTests
{
    [Fact]
    public void CalculateRelativeLuminance_BlackAndWhite_ReturnsExtremes()
    {
        var blackLum = LuminanceCalculator.CalculateRelativeLuminance(ColorRgba.Black);
        var whiteLum = LuminanceCalculator.CalculateRelativeLuminance(ColorRgba.White);

        blackLum.Should().BeApproximately(0.0, 0.001);
        whiteLum.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void CalculateRelativeLuminance_PrimaryColors_MatchesWcagWeights()
    {
        var redLum = LuminanceCalculator.CalculateRelativeLuminance(new ColorRgba(255, 0, 0));
        var greenLum = LuminanceCalculator.CalculateRelativeLuminance(new ColorRgba(0, 255, 0));
        var blueLum = LuminanceCalculator.CalculateRelativeLuminance(new ColorRgba(0, 0, 255));

        // Green has highest perceived luminance (0.7152), Red is second (0.2126), Blue is lowest (0.0722)
        greenLum.Should().BeGreaterThan(redLum);
        redLum.Should().BeGreaterThan(blueLum);
    }

    [Fact]
    public void CalculateContrastRatio_BlackAndWhite_Returns21()
    {
        var contrast = LuminanceCalculator.CalculateContrastRatio(ColorRgba.White, ColorRgba.Black);
        contrast.Should().BeApproximately(21.0, 0.1);
    }

    [Fact]
    public void CalculateContrastRatio_IdenticalColors_Returns1()
    {
        var color = new ColorRgba(120, 150, 200);
        var contrast = LuminanceCalculator.CalculateContrastRatio(color, color);
        contrast.Should().BeApproximately(1.0, 0.01);
    }

    [Fact]
    public void CalculateAutoTitleColor_OnDarkBackground_PicksLightTitle()
    {
        var darkBg = new ColorRgba(15, 23, 42); // Slate 900
        var titleColor = LuminanceCalculator.CalculateAutoTitleColor(darkBg);

        titleColor.Should().Be(ColorRgba.LightTitle);
    }

    [Fact]
    public void CalculateAutoTitleColor_OnLightBackground_PicksDarkTitle()
    {
        var lightBg = new ColorRgba(243, 244, 246); // Gray 100
        var titleColor = LuminanceCalculator.CalculateAutoTitleColor(lightBg);

        titleColor.Should().Be(ColorRgba.DarkTitle);
    }

    [Theory]
    [InlineData("Auto", true)]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("#FF0000", false)]
    public void ResolveTitleColor_VariousInputs_ResolvesAppropriately(string setting, bool isAuto)
    {
        var darkBg = new ColorRgba(20, 20, 20);
        var resolved = LuminanceCalculator.ResolveTitleColor(setting, darkBg);

        if (isAuto)
        {
            resolved.Should().Be(ColorRgba.LightTitle);
        }
        else if (setting == "Light")
        {
            resolved.Should().Be(ColorRgba.LightTitle);
        }
        else if (setting == "Dark")
        {
            resolved.Should().Be(ColorRgba.DarkTitle);
        }
        else if (setting == "#FF0000")
        {
            resolved.Should().Be(new ColorRgba(255, 0, 0));
        }
    }
}
