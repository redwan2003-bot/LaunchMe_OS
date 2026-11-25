namespace LaunchMe.Contracts.Tiling;

/// <summary>
/// Service managing application and window filtering rules.
/// </summary>
public interface IWindowRuleService
{
    void AddRule(WindowRule rule);
    bool RemoveRule(string ruleId);
    bool ShouldFloat(string processName, string title);
    int? GetTargetWorkspace(string processName, string title);
    IReadOnlyList<WindowRule> GetAllRules();
}
