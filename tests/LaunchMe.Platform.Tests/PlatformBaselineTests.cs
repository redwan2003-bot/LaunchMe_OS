using Xunit;

namespace LaunchMe.Platform.Tests;

public class PlatformBaselineTests
{
    [Fact]
    public void PlatformModule_ShouldHaveCorrectName()
    {
        Assert.Equal("LaunchMe.Platform.Win32", Platform.Win32.PlatformModule.ModuleName);
    }
}
