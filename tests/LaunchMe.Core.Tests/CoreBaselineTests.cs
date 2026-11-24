using Xunit;

namespace LaunchMe.Core.Tests;

public class CoreBaselineTests
{
    [Fact]
    public void CoreModule_ShouldHaveCorrectName()
    {
        Assert.Equal("LaunchMe.Core", CoreModule.ModuleName);
    }
}
