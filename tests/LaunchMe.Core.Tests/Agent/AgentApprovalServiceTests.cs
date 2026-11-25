using FluentAssertions;
using LaunchMe.Contracts.Agent;
using LaunchMe.Core.Agent;
using Xunit;

namespace LaunchMe.Core.Tests.Agent;

public class AgentApprovalServiceTests
{
    [Fact]
    public async Task RequestApproval_WhenNotAutoApproved_AwaitsUserDecision()
    {
        var service = new AgentApprovalService();
        AgentApprovalRequest? receivedRequest = null;
        service.ApprovalRequested += (_, r) => receivedRequest = r;

        var request = new AgentApprovalRequest(
            "req-1",
            "Claude Code",
            "Bash",
            "git push origin main",
            null,
            ApprovalRiskLevel.High,
            null,
            DateTime.UtcNow);

        var task = service.RequestApprovalAsync(request);

        receivedRequest.Should().NotBeNull();
        receivedRequest!.RequestId.Should().Be("req-1");

        // Submit approval
        await service.SubmitDecisionAsync("req-1", ApprovalAction.Allowed);
        var decision = await task;

        decision.RequestId.Should().Be("req-1");
        decision.Action.Should().Be(ApprovalAction.Allowed);
    }

    [Fact]
    public async Task RequestApproval_WhenAutoApproved_ReturnsAllowedImmediately()
    {
        var service = new AgentApprovalService();
        service.SetAutoApprovalRule("Bash", "^git status$", alwaysAllow: true);

        var request = new AgentApprovalRequest(
            "req-2",
            "AGY",
            "Bash",
            "git status",
            null,
            ApprovalRiskLevel.Safe,
            null,
            DateTime.UtcNow);

        var decision = await service.RequestApprovalAsync(request);

        decision.Action.Should().Be(ApprovalAction.Allowed);
    }

    [Fact]
    public async Task SubmitDecision_WithRememberRule_RegistersAutoApprovalRule()
    {
        var service = new AgentApprovalService();

        var request = new AgentApprovalRequest(
            "req-3",
            "Claude Code",
            "Bash",
            "dotnet test",
            null,
            ApprovalRiskLevel.Moderate,
            null,
            DateTime.UtcNow);

        var task = service.RequestApprovalAsync(request);

        await service.SubmitDecisionAsync("req-3", ApprovalAction.RememberRule, rememberRulePattern: "^dotnet test$");
        var decision = await task;

        decision.Action.Should().Be(ApprovalAction.Allowed);
        service.IsAutoApproved("Bash", "dotnet test").Should().BeTrue();
    }
}
