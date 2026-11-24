using FluentAssertions;
using LaunchMe.Core.Search;
using Xunit;

namespace LaunchMe.Core.Tests.Search;

public sealed class InlineCalculatorServiceTests
{
    private readonly InlineCalculatorService _calculator = new();

    [Theory]
    [InlineData("2 + 2", 4.0)]
    [InlineData("10 - 3 * 2", 4.0)]
    [InlineData("(10 - 3) * 2", 14.0)]
    [InlineData("2 ^ 3", 8.0)]
    [InlineData("10 % 3", 1.0)]
    [InlineData("sqrt(16)", 4.0)]
    [InlineData("abs(-42)", 42.0)]
    [InlineData("3.5 + 1.5", 5.0)]
    public void Calculate_ValidExpressions_ReturnsExpectedValue(string expr, double expected)
    {
        var result = _calculator.Evaluate(expr);
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Result.Should().BeApproximately(expected, 0.0001);
    }

    [Theory]
    [InlineData("invalid + expression")]
    [InlineData("10 / 0")]
    [InlineData("((10 + 2)")]
    public void Calculate_InvalidExpressions_ReturnsInvalid(string expr)
    {
        var result = _calculator.Evaluate(expr);
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
    }
}
