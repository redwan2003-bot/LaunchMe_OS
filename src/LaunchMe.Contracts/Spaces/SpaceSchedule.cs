using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Spaces;

public sealed class SpaceSchedule
{
    [JsonPropertyName("days")]
    public List<string> Days { get; set; } = ["Mon", "Tue", "Wed", "Thu", "Fri"];

    [JsonPropertyName("start")]
    public string Start { get; set; } = "09:00";

    [JsonPropertyName("end")]
    public string End { get; set; } = "18:00";
}
