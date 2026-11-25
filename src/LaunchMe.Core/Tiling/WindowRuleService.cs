using System.Text.RegularExpressions;
using LaunchMe.Contracts.Tiling;

namespace LaunchMe.Core.Tiling;

/// <summary>
/// Manages application-specific rules for floating, sizing, and workspace placement.
/// </summary>
public class WindowRuleService : IWindowRuleService
{
    private readonly List<WindowRule> _rules = new();
    private readonly object _lock = new();

    public WindowRuleService()
    {
        // Seed standard default floating rules
        AddDefaultRules();
    }

    public void AddRule(WindowRule rule)
    {
        lock (_lock)
        {
            _rules.RemoveAll(r => r.Id == rule.Id);
            _rules.Add(rule);
        }
    }

    public bool RemoveRule(string ruleId)
    {
        lock (_lock)
        {
            return _rules.RemoveAll(r => r.Id == ruleId) > 0;
        }
    }

    public bool ShouldFloat(string processName, string title)
    {
        lock (_lock)
        {
            foreach (var rule in _rules)
            {
                if (rule.ForceFloat && Matches(rule, processName, title))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public int? GetTargetWorkspace(string processName, string title)
    {
        lock (_lock)
        {
            foreach (var rule in _rules)
            {
                if (rule.AssignedWorkspace.HasValue && Matches(rule, processName, title))
                {
                    return rule.AssignedWorkspace.Value;
                }
            }
        }
        return null;
    }

    public IReadOnlyList<WindowRule> GetAllRules()
    {
        lock (_lock)
        {
            return _rules.ToList();
        }
    }

    private static bool Matches(WindowRule rule, string processName, string title)
    {
        if (!string.IsNullOrWhiteSpace(rule.ProcessPattern))
        {
            if (!Regex.IsMatch(processName, rule.ProcessPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(rule.TitlePattern))
        {
            if (!Regex.IsMatch(title, rule.TitlePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return false;
            }
        }

        return true;
    }

    private void AddDefaultRules()
    {
        AddRule(new WindowRule("calc", "^calculator.*$", null, true, null, null));
        AddRule(new WindowRule("settings", "^systemsettings$", null, true, null, null));
        AddRule(new WindowRule("taskmgr", "^taskmgr$", null, true, null, null));
        AddRule(new WindowRule("1password", "^1password.*$", null, true, null, null));
        AddRule(new WindowRule("dialogs", null, "^(open|save|properties|settings|about)$", true, null, null));
    }
}
