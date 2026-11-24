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
}
