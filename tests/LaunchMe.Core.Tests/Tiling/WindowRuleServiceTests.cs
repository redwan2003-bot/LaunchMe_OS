using FluentAssertions;
using LaunchMe.Contracts.Tiling;
using LaunchMe.Core.Tiling;
using Xunit;

namespace LaunchMe.Core.Tests.Tiling;

public class WindowRuleServiceTests
{
    [Fact]
    public void DefaultRules_Calculator_ShouldFloat()
    {
        var service = new WindowRuleService();

        service.ShouldFloat("CalculatorApp", "Calculator").Should().BeTrue();
        service.ShouldFloat("calculator.exe", "Calc").Should().BeTrue();
    }

    [Fact]
    public void DefaultRules_StandardApp_ShouldNotFloat()
    {
        var service = new WindowRuleService();

        service.ShouldFloat("code", "Visual Studio Code").Should().BeFalse();
        service.ShouldFloat("windowsTerminal", "PowerShell").Should().BeFalse();
    }

    [Fact]
    public void CustomRule_ByTitle_ForcesFloat()
    {
        var service = new WindowRuleService();
        service.AddRule(new WindowRule("custom", null, ".*Preferences.*", true, null, null));

        service.ShouldFloat("myApp", "Global Preferences").Should().BeTrue();
        service.ShouldFloat("myApp", "Main Workspace").Should().BeFalse();
    }

    [Fact]
    public void CustomRule_WorkspaceAffinity_ReturnsTargetWorkspace()
    {
        var service = new WindowRuleService();
        service.AddRule(new WindowRule("slack-space", "^slack$", null, false, 3, null));

        service.GetTargetWorkspace("slack", "Team Chat").Should().Be(3);
        service.GetTargetWorkspace("chrome", "Browser").Should().BeNull();
    }
}
