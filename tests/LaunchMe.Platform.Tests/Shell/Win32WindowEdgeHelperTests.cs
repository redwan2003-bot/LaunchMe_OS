using FluentAssertions;
using LaunchMe.Platform.Win32.Shell;
using Xunit;

namespace LaunchMe.Platform.Tests.Shell;

public class Win32WindowEdgeHelperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0x12345678)]
    public void ApplyBorderlessPillDwmChrome_ZeroOrInvalidHwnd_ReturnsFalseWithoutThrowing(long rawHwnd)
    {
        bool result = Win32WindowEdgeHelper.ApplyBorderlessPillDwmChrome(new nint(rawHwnd));

        result.Should().BeFalse();
    }

    [Fact]
    public void GetActiveMonitorBounds_ReturnsPositiveMonitorAndWorkAreaDimensions()
    {
        var bounds = Win32WindowEdgeHelper.GetActiveMonitorBounds();

        bounds.Width.Should().BeGreaterThan(0);
        bounds.Height.Should().BeGreaterThan(0);
        bounds.WorkWidth.Should().BeGreaterThan(0);
        bounds.WorkHeight.Should().BeGreaterThan(0);
        bounds.Right.Should().BeGreaterThan(bounds.Left);
        bounds.Bottom.Should().BeGreaterThan(bounds.Top);
    }
}
