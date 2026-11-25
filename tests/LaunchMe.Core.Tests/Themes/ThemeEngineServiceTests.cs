using FluentAssertions;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Core.Themes;
using Xunit;

namespace LaunchMe.Core.Tests.Themes;

public sealed class ThemeEngineServiceTests
{
    private readonly IThemeEngineService _service = new ThemeEngineService();

    [Fact]
    public void ThemeEngineService_EndToEnd_WorksCorrectly()
    {
        var settings = new ThemeSettings
        {
            Style = "LiquidGlass",
            Mode = "Tinted",
            TintHex = "#3B82F6",
            Intensity = 0.8,
            TitleColor = "Auto"
        };

        var res = _service.ResolveTheme(settings);
        res.Should().NotBeNull();
        res.IsLiquidGlass.Should().BeTrue();

        var color = _service.ParseColor("#3B82F6");
        color.R.Should().Be(59);
        color.G.Should().Be(130);
        color.B.Should().Be(246);

        var lum = _service.CalculateLuminance(color);
        lum.Should().BeInRange(0.0, 1.0);

        var contrast = _service.CalculateContrastRatio(ColorRgba.White, ColorRgba.Black);
        contrast.Should().BeApproximately(21.0, 0.1);

        var title = _service.ResolveAutoTitleColor(color);
        title.Should().NotBe(default);
    }

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.85, 0.85)]
    [InlineData(1.0, 1.0)]
    [InlineData(2.5, 1.0)]
    [InlineData(double.NaN, 0.85)]
    public void ResolveTheme_ClampsSpecularIntensityToUnitRange(double inputSpecular, double expectedSpecular)
    {
        var settings = new ThemeSettings
        {
            SpecularIntensity = inputSpecular
        };

        var resolution = _service.ResolveTheme(settings);

        resolution.SpecularIntensity.Should().Be(expectedSpecular);
        resolution.EnableGrain.Should().BeTrue();
    }

    [Fact]
    public void NormalizeSettings_PreservesExplicitEnableGrainAndClampsValues()
    {
        var settings = new ThemeSettings
        {
            Intensity = 1.75,
            SpecularIntensity = -2.0,
            EnableGrain = false
        };

        var normalized = ThemeEngineService.NormalizeSettings(settings);

        normalized.Intensity.Should().Be(1.0);
        normalized.SpecularIntensity.Should().Be(0.0);
        normalized.EnableGrain.Should().BeFalse();
    }
}
