namespace LaunchMe.Contracts.Agent;

public record AgentApprovalRequest(
    string RequestId,
    string AgentName,
    string ToolName,
    string CommandOrAction,
    string? FileDiffUnified,
    ApprovalRiskLevel Risk,
    IReadOnlyList<string>? MultipleChoiceOptions,
    DateTime Timestamp
);
