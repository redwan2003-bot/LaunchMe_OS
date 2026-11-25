namespace LaunchMe.Contracts.Agent;

public interface IAgentApprovalService
{
    event EventHandler<AgentApprovalRequest> ApprovalRequested;
    event EventHandler<string> ApprovalResolved;

    Task SubmitDecisionAsync(string requestId, ApprovalAction action, string? userResponseText = null, string? rememberRulePattern = null);
    IReadOnlyList<AgentApprovalRequest> GetPendingRequests();
    void SetAutoApprovalRule(string toolName, string pattern, bool alwaysAllow);
    bool IsAutoApproved(string toolName, string commandOrAction);
}
