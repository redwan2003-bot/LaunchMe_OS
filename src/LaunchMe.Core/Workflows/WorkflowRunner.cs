using System.Diagnostics;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Contracts.Workflows;

namespace LaunchMe.Core.Workflows;

public sealed class WorkflowRunner : IWorkflowRunner
{
    private readonly IAppLauncherService? _appLauncher;

    public WorkflowRunner() : this(null)
    {
    }

    public WorkflowRunner(IAppLauncherService? appLauncher)
    {
        _appLauncher = appLauncher;
    }

    /// <summary>
    /// Validates a workflow definition and returns human-readable error messages for invalid entries.
    /// </summary>
    public IReadOnlyList<string> ValidateWorkflow(WorkflowDefinition workflow)
    {
        var errors = new List<string>();
        if (workflow is null)
        {
            errors.Add("Workflow definition cannot be null.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(workflow.Name))
        {
            errors.Add("Workflow name is required.");
        }

        if (workflow.Actions is null || workflow.Actions.Count == 0)
        {
            errors.Add("Workflow must contain at least one action.");
            return errors;
        }

        for (var i = 0; i < workflow.Actions.Count; i++)
        {
            var action = workflow.Actions[i];
            if (action is null)
            {
                errors.Add($"Action at index {i} cannot be null.");
                continue;
            }

            if (action.DelayMs < 0)
            {
                errors.Add($"Action at index {i} has a negative delay ({action.DelayMs} ms).");
            }

            if (action.ActionType == WorkflowActionType.Delay)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(action.Target))
            {
                errors.Add($"Action '{action.ActionType}' at index {i} requires a non-empty target.");
                continue;
            }

            if (action.ActionType == WorkflowActionType.OpenUrl && !IsAllowedHttpUrl(action.Target, out _))
            {
                errors.Add($"Action 'OpenUrl' at index {i} uses an invalid or disallowed URL scheme: '{action.Target}'. Only http and https are permitted.");
            }
        }

        return errors;
    }

    public async Task ExecuteWorkflowAsync(WorkflowDefinition workflow, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        foreach (var action in workflow.Actions)
        {
            ct.ThrowIfCancellationRequested();

            if (action.DelayMs > 0)
            {
                await Task.Delay(action.DelayMs, ct).ConfigureAwait(false);
            }

            switch (action.ActionType)
            {
                case WorkflowActionType.LaunchApp:
                    if (!string.IsNullOrWhiteSpace(action.Target))
                    {
                        if (_appLauncher is not null)
                        {
                            _appLauncher.LaunchTarget(action.Target, action.Arguments);
                        }
                        else
                        {
                            var psi = new ProcessStartInfo(action.Target)
                            {
                                Arguments = action.Arguments ?? string.Empty,
                                UseShellExecute = true
                            };
                            Process.Start(psi);
                        }
                    }
                    break;

                case WorkflowActionType.OpenFile:
                case WorkflowActionType.OpenFolder:
                    if (!string.IsNullOrWhiteSpace(action.Target))
                    {
                        Process.Start(new ProcessStartInfo(action.Target)
                        {
                            UseShellExecute = true
                        });
                    }
                    break;

                case WorkflowActionType.OpenUrl:
                    if (!string.IsNullOrWhiteSpace(action.Target) &&
                        IsAllowedHttpUrl(action.Target, out var uri) &&
                        uri is not null)
                    {
                        Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
                        {
                            UseShellExecute = true
                        });
                    }
                    break;

                case WorkflowActionType.Delay:
                    // Handled above by action.DelayMs
                    break;
            }
        }
    }

    private static bool IsAllowedHttpUrl(string target, out Uri? validatedUri)
    {
        validatedUri = null;
        var trimmed = target.Trim();
        // Security check: Only permit valid absolute http/https URLs to prevent arbitrary command execution or dangerous URI schemes
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            validatedUri = uri;
            return true;
        }

        return false;
    }
}
