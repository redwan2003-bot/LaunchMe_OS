using FluentAssertions;
using LaunchMe.Core.Themes;
using Xunit;

namespace LaunchMe.Core.Tests.Themes;

public sealed class ColorParserTests
{
    [Theory]
    [InlineData("#FFF", 255, 255, 255, 255)]
    [InlineData("FFF", 255, 255, 255, 255)]
    [InlineData("#000", 0, 0, 0, 255)]
    [InlineData("#F00", 255, 0, 0, 255)]
    [InlineData("#0F0", 0, 255, 0, 255)]
    [InlineData("#00F", 0, 0, 255, 255)]
    public void TryParseHex_Valid3Digit_ReturnsExpected(string input, byte r, byte g, byte b, byte a)
    {
        var success = ColorParser.TryParseHex(input, out var color);

        success.Should().BeTrue();
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
        color.A.Should().Be(a);
    }

    [Theory]
    [InlineData("#8FFF", 255, 255, 255, 136)] // 0x88 = 136
    [InlineData("0F00", 255, 0, 0, 0)]
    public void TryParseHex_Valid4DigitArgb_ReturnsExpected(string input, byte r, byte g, byte b, byte a)
    {
        var success = ColorParser.TryParseHex(input, out var color);

        success.Should().BeTrue();
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
        color.A.Should().Be(a);
    }

    [Theory]
    [InlineData("#1E40AF", 30, 64, 175, 255)]
    [InlineData("1E40AF", 30, 64, 175, 255)]
    [InlineData("#FFFFFF", 255, 255, 255, 255)]
    [InlineData("#000000", 0, 0, 0, 255)]
    public void TryParseHex_Valid6Digit_ReturnsExpected(string input, byte r, byte g, byte b, byte a)
    {
        var success = ColorParser.TryParseHex(input, out var color);

        success.Should().BeTrue();
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
        color.A.Should().Be(a);
    }

    [Theory]
    [InlineData("#801E40AF", 30, 64, 175, 128)]
    [InlineData("FF112233", 17, 34, 51, 255)]
    [InlineData("#00FFFFFF", 255, 255, 255, 0)]
    public void TryParseHex_Valid8DigitArgb_ReturnsExpected(string input, byte r, byte g, byte b, byte a)
    {
        var success = ColorParser.TryParseHex(input, out var color);

        success.Should().BeTrue();
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
        color.A.Should().Be(a);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#123456789")]
    [InlineData("NOT_HEX")]
    public void TryParseHex_InvalidInput_ReturnsFalse(string? input)
    {
        var success = ColorParser.TryParseHex(input, out var color);

        success.Should().BeFalse();
        color.Should().Be(default(ColorRgba));
    }

    [Fact]
    public void ParseHex_InvalidInput_ThrowsFormatException()
    {
        var act = () => ColorParser.ParseHex("invalid");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ColorRgba_ToHex_And_ToRgbaCss_FormatCorrectly()
    {
        var color = new ColorRgba(30, 64, 175, 255);
        color.ToHex(includeAlpha: false).Should().Be("#1E40AF");
        color.ToHex(includeAlpha: true, argbOrder: true).Should().Be("#FF1E40AF");

        var translucent = color.WithAlpha(0.5);
        translucent.A.Should().Be(128);
        translucent.ToRgbaCss().Should().Be("rgba(30, 64, 175, 0.502)");
    }

    [Fact]
    public void ColorRgba_BlendOver_CalculatesAlphaCompositing()
    {
        var transparentBlue = new ColorRgba(0, 0, 255, 128);
        var white = new ColorRgba(255, 255, 255, 255);

        var blended = transparentBlue.BlendOver(white);

        blended.A.Should().Be(255);
        blended.R.Should().BeLessThan(255);
        blended.B.Should().BeGreaterThan(0);
    }
}
