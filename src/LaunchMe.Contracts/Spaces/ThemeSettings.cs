using System.Text.Json.Serialization;

namespace LaunchMe.Contracts.Spaces;

public sealed class ThemeSettings
{
    [JsonPropertyName("style")]
    public string Style { get; set; } = "LiquidGlass"; // "LiquidGlass" | "Flat"

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "Tinted"; // "Clear" | "Tinted"

    [JsonPropertyName("tintHex")]
    public string TintHex { get; set; } = "#1E40AF";

    [JsonPropertyName("intensity")]
    public double Intensity { get; set; } = 0.65;

    [JsonPropertyName("titleColor")]
    public string TitleColor { get; set; } = "Auto";

    [JsonPropertyName("specularIntensity")]
    public double SpecularIntensity { get; set; } = 0.85;

    [JsonPropertyName("enableGrain")]
    public bool EnableGrain { get; set; } = true;
}
