namespace LaunchMe.Contracts.Agent;

public record AgentApprovalDecision(
    string RequestId,
    ApprovalAction Action,
    string? UserResponseText,
    string? RememberRulePattern
);
