namespace LaunchMe.Contracts.Workflows;

/// <summary>
/// Executes configured multi-target batch launch workflows.
/// </summary>
public interface IWorkflowRunner
{
    Task ExecuteWorkflowAsync(WorkflowDefinition workflow, CancellationToken ct = default);
}
