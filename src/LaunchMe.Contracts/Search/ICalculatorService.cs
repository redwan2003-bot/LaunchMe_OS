namespace LaunchMe.Contracts.Search;

/// <summary>
/// Evaluates inline math expressions with support for arithmetic, trigonometric, and scientific functions.
/// </summary>
public interface ICalculatorService
{
    CalculationResult? Evaluate(string expression);
}
