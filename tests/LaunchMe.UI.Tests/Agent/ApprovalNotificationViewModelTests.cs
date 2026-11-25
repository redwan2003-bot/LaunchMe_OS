using FluentAssertions;
using LaunchMe.Contracts.Agent;
using LaunchMe.Core.Agent;
using LaunchMe.UI.Agent;
using Xunit;

namespace LaunchMe.UI.Tests.Agent;

public class ApprovalNotificationViewModelTests
{
    [Fact]
    public void InitialState_NoPendingApproval_HasHiddenCard()
    {
        var approvalService = new AgentApprovalService();
        var diffParser = new DiffParserService();
        var vm = new ApprovalNotificationViewModel(approvalService, diffParser);

        vm.HasPendingApproval.Should().BeFalse();
        vm.ActiveRequest.Should().BeNull();
        vm.ApprovalCardVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Collapsed);
    }

    [Fact]
    public void ApprovalRequested_SetsActiveRequestAndVisibleCard()
    {
        var approvalService = new AgentApprovalService();
        var diffParser = new DiffParserService();
        var vm = new ApprovalNotificationViewModel(approvalService, diffParser);

        var request = new AgentApprovalRequest(
            "req-ui-1",
            "Claude Code",
            "Bash",
            "git push",
            null,
            ApprovalRiskLevel.High,
            null,
            DateTime.UtcNow);

        _ = approvalService.RequestApprovalAsync(request);

        vm.HasPendingApproval.Should().BeTrue();
        vm.ActiveRequest.Should().Be(request);
        vm.ApprovalCardVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Visible);
    }

    [Fact]
    public async Task ApproveCommand_ResolvesRequest_And_HidesCard()
    {
        var approvalService = new AgentApprovalService();
        var diffParser = new DiffParserService();
        var vm = new ApprovalNotificationViewModel(approvalService, diffParser);

        var request = new AgentApprovalRequest(
            "req-ui-2",
            "AGY",
            "Bash",
            "npm start",
            null,
            ApprovalRiskLevel.Safe,
            null,
            DateTime.UtcNow);

        var task = approvalService.RequestApprovalAsync(request);

        vm.ApproveCommand.Execute(null);

        var decision = await task;
        decision.Action.Should().Be(ApprovalAction.Allowed);
        vm.HasPendingApproval.Should().BeFalse();
        vm.ApprovalCardVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Collapsed);
    }
}
