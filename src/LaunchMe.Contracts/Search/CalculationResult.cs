namespace LaunchMe.Contracts.Search;

public sealed class CalculationResult
{
    public string Expression { get; set; } = string.Empty;
    public double Result { get; set; }
    public string FormattedResult { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}
