using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Workflows;

public enum WorkflowActionType
{
    LaunchApp,
    OpenFile,
    OpenFolder,
    OpenUrl,
    Delay
}

public sealed class WorkflowAction
{
    [JsonPropertyName("actionType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WorkflowActionType ActionType { get; set; } = WorkflowActionType.LaunchApp;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    [JsonPropertyName("delayMs")]
    public int DelayMs { get; set; }
}
