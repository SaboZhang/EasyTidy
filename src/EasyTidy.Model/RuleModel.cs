
namespace EasyTidy.Model;

public class RuleModel
{
    public string Rule { get; set; }

    public TaskRuleType RuleType { get; set; }

    public FilterTable Filter { get; set; }
}