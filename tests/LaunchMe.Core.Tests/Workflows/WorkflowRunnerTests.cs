using FluentAssertions;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Contracts.Workflows;
using LaunchMe.Core.Workflows;
using Moq;
using Xunit;

namespace LaunchMe.Core.Tests.Workflows;

public sealed class WorkflowRunnerTests
{
    [Fact]
    public async Task ExecuteWorkflowAsync_SequentialLaunchAppActions_DelegatesInOrder()
    {
        var calls = new List<(string Target, string? Arguments)>();
        var launcher = new Mock<IAppLauncherService>();
        launcher.Setup(l => l.LaunchTarget(It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string?>((target, args) => calls.Add((target, args)))
            .Returns(true);

        var runner = new WorkflowRunner(launcher.Object);
        var workflow = new WorkflowDefinition
        {
            Name = "Morning Dev",
            Actions =
            [
                new WorkflowAction { ActionType = WorkflowActionType.LaunchApp, Target = "code.exe", Arguments = "." },
                new WorkflowAction { ActionType = WorkflowActionType.LaunchApp, Target = "wt.exe", Arguments = "--profile pwsh" }
            ]
        };

        await runner.ExecuteWorkflowAsync(workflow);

        calls.Should().HaveCount(2);
        calls[0].Should().Be(("code.exe", "."));
        calls[1].Should().Be(("wt.exe", "--profile pwsh"));
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_DelayAction_WaitsBeforeNextAction()
    {
        var timestamps = new List<DateTimeOffset>();
        var launcher = new Mock<IAppLauncherService>();
        launcher.Setup(l => l.LaunchTarget(It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string?>((_, _) => timestamps.Add(DateTimeOffset.UtcNow))
            .Returns(true);

        var runner = new WorkflowRunner(launcher.Object);
        var workflow = new WorkflowDefinition
        {
            Name = "Delayed Launch",
            Actions =
            [
                new WorkflowAction { ActionType = WorkflowActionType.LaunchApp, Target = "first.exe" },
                new WorkflowAction { ActionType = WorkflowActionType.Delay, DelayMs = 60 },
                new WorkflowAction { ActionType = WorkflowActionType.LaunchApp, Target = "second.exe" }
            ]
        };

        await runner.ExecuteWorkflowAsync(workflow);

        timestamps.Should().HaveCount(2);
        (timestamps[1] - timestamps[0]).TotalMilliseconds.Should().BeGreaterThanOrEqualTo(40);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WhenCancelledDuringDelay_ThrowsOperationCanceledException()
    {
        var launcher = new Mock<IAppLauncherService>();
        var runner = new WorkflowRunner(launcher.Object);

        var workflow = new WorkflowDefinition
        {
            Name = "Cancellable",
            Actions =
            [
                new WorkflowAction { ActionType = WorkflowActionType.Delay, DelayMs = 5000 },
                new WorkflowAction { ActionType = WorkflowActionType.LaunchApp, Target = "never.exe" }
            ]
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        Func<Task> act = () => runner.ExecuteWorkflowAsync(workflow, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        launcher.Verify(l => l.LaunchTarget(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public void ValidateWorkflow_ValidDefinition_ReturnsNoErrors()
    {
        var runner = new WorkflowRunner();
        var workflow = new WorkflowDefinition
        {
            Name = "Valid Workflow",
            Actions =
            [
                new WorkflowAction { ActionType = WorkflowActionType.LaunchApp, Target = "notepad.exe" },
                new WorkflowAction { ActionType = WorkflowActionType.Delay, DelayMs = 100 },
                new WorkflowAction { ActionType = WorkflowActionType.OpenUrl, Target = "https://example.com" }
            ]
        };

        var errors = runner.ValidateWorkflow(workflow);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateWorkflow_InvalidInputs_ReturnsExpectedErrors()
    {
        var runner = new WorkflowRunner();

        var emptyWorkflow = new WorkflowDefinition
        {
            Name = "   ",
            Actions = []
        };
        var emptyErrors = runner.ValidateWorkflow(emptyWorkflow);
        emptyErrors.Should().HaveCount(2);

        var dangerousWorkflow = new WorkflowDefinition
        {
            Name = "Unsafe",
            Actions =
            [
                new WorkflowAction { ActionType = WorkflowActionType.LaunchApp, Target = "" },
                new WorkflowAction { ActionType = WorkflowActionType.OpenUrl, Target = "javascript:alert(1)" },
                new WorkflowAction { ActionType = WorkflowActionType.OpenUrl, Target = "file:///C:/Windows/System32/cmd.exe" },
                new WorkflowAction { ActionType = WorkflowActionType.Delay, DelayMs = -10 }
            ]
        };

        var dangerousErrors = runner.ValidateWorkflow(dangerousWorkflow);
        dangerousErrors.Should().HaveCount(4);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_OpenUrlRejectsDangerousSchemes_EvenWithMockedLauncher()
    {
        var launcher = new Mock<IAppLauncherService>();
        var runner = new WorkflowRunner(launcher.Object);

        var workflow = new WorkflowDefinition
        {
            Name = "Dangerous URL",
            Actions =
            [
                new WorkflowAction { ActionType = WorkflowActionType.OpenUrl, Target = "ms-settings:windowsupdate" },
                new WorkflowAction { ActionType = WorkflowActionType.OpenUrl, Target = "file:///C:/test.txt" }
            ]
        };

        await runner.ExecuteWorkflowAsync(workflow);

        launcher.Verify(l => l.LaunchTarget(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }
}
