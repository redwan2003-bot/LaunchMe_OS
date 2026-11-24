using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Workflows;

public sealed class WorkflowDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("actions")]
    public List<WorkflowAction> Actions { get; set; } = [];
}
