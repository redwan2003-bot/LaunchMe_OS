using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using LaunchMe.Contracts.Agent;

namespace LaunchMe.Core.Agent;

/// <summary>
/// Thread-safe service coordinating agent permission approvals and automated rule matching.
/// </summary>
public class AgentApprovalService : IAgentApprovalService
{
    private readonly ConcurrentDictionary<string, AgentApprovalRequest> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<AgentApprovalDecision>> _pendingCompletions = new();
    private readonly ConcurrentDictionary<string, List<string>> _autoApprovalRules = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<AgentApprovalRequest>? ApprovalRequested;
    public event EventHandler<string>? ApprovalResolved;

    public async Task<AgentApprovalDecision> RequestApprovalAsync(AgentApprovalRequest request, CancellationToken ct = default)
    {
        if (IsAutoApproved(request.ToolName, request.CommandOrAction))
        {
            return new AgentApprovalDecision(request.RequestId, ApprovalAction.Allowed, null, null);
        }

        var tcs = new TaskCompletionSource<AgentApprovalDecision>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[request.RequestId] = request;
        _pendingCompletions[request.RequestId] = tcs;

        ApprovalRequested?.Invoke(this, request);

        using var reg = ct.Register(() =>
        {
            if (_pendingCompletions.TryRemove(request.RequestId, out var pendingTcs))
            {
                _pendingRequests.TryRemove(request.RequestId, out _);
                pendingTcs.TrySetResult(new AgentApprovalDecision(request.RequestId, ApprovalAction.Denied, "Cancelled/Timed out", null));
            }
        });

        return await tcs.Task;
    }

    public Task SubmitDecisionAsync(string requestId, ApprovalAction action, string? userResponseText = null, string? rememberRulePattern = null)
    {
        if (_pendingCompletions.TryRemove(requestId, out var tcs))
        {
            _pendingRequests.TryRemove(requestId, out var request);

            if (action == ApprovalAction.RememberRule && request != null)
            {
                string pattern = !string.IsNullOrWhiteSpace(rememberRulePattern)
                    ? rememberRulePattern
                    : Regex.Escape(request.CommandOrAction);

                SetAutoApprovalRule(request.ToolName, pattern, alwaysAllow: true);
                action = ApprovalAction.Allowed;
            }

            var decision = new AgentApprovalDecision(requestId, action, userResponseText, rememberRulePattern);
            tcs.TrySetResult(decision);
            ApprovalResolved?.Invoke(this, requestId);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<AgentApprovalRequest> GetPendingRequests()
    {
        return _pendingRequests.Values.OrderBy(r => r.Timestamp).ToList();
    }

    public void SetAutoApprovalRule(string toolName, string pattern, bool alwaysAllow)
    {
        if (string.IsNullOrWhiteSpace(toolName) || string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        _autoApprovalRules.AddOrUpdate(
            toolName,
            _ => new List<string> { pattern },
            (_, list) =>
            {
                lock (list)
                {
                    if (!list.Contains(pattern))
                    {
                        list.Add(pattern);
                    }
                }
                return list;
            });
    }

    public bool IsAutoApproved(string toolName, string commandOrAction)
    {
        if (string.IsNullOrWhiteSpace(toolName) || string.IsNullOrWhiteSpace(commandOrAction))
        {
            return false;
        }

        if (_autoApprovalRules.TryGetValue(toolName, out var patterns))
        {
            lock (patterns)
            {
                foreach (var pattern in patterns)
                {
                    try
                    {
                        if (Regex.IsMatch(commandOrAction, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Invalid regex pattern is safely ignored
                    }
                }
            }
        }

        return false;
    }
}
