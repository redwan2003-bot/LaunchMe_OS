using System.Diagnostics;
using LaunchMe.Contracts.Workflows;

namespace LaunchMe.Core.Workflows;

public sealed class WorkflowRunner : IWorkflowRunner
{
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
                        var psi = new ProcessStartInfo(action.Target)
                        {
                            Arguments = action.Arguments ?? string.Empty,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    break;

                case WorkflowActionType.OpenFile:
                case WorkflowActionType.OpenFolder:
                case WorkflowActionType.OpenUrl:
                    if (!string.IsNullOrWhiteSpace(action.Target))
                    {
                        Process.Start(new ProcessStartInfo(action.Target)
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
}
