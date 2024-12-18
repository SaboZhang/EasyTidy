using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EasyTidy.Util.UtilInterface;

namespace EasyTidy.Util.Strategy;

public class FileFilterStrategy : IFilterStrategy
{
    private readonly Dictionary<Func<string, bool>, Func<string, IEnumerable<Func<string, bool>>>> _conditionHandlers;

    public FileFilterStrategy()
    {
        _conditionHandlers = new Dictionary<Func<string, bool>, Func<string, IEnumerable<Func<string, bool>>>>
        {
            { rule => rule.Contains('/'), GenerateIncludeExcludeFilter },
            { rule => rule.StartsWith('*') && rule.EndsWith('*'), GenerateContainsKeywordFilter },
            { rule => rule.Contains('*') && rule.Contains('.'), MatchesStartsWithAndEndsWith },
            { rule => rule.EndsWith('*'), GenerateStartsWithPrefixFilter },
            { rule => rule.StartsWith('*'), GenerateExtensionFilter },
            { rule => true, _ => new Func<string, bool>[] { _ => false } } // 默认规则

        };
    }

    public IEnumerable<Func<string, bool>> GenerateFilters(string rule)
    {
        // 如果规则是 "*"，则返回一个文件存在检查器
        if (IsWildcardRule(rule))
        {
            yield return filePath => File.Exists(filePath);
        }
        else if (IsNegatedRule(rule))
        {
            foreach (var filter in ProcessNegatedRule(rule))
            {
                yield return filePath => !filter(filePath);
            }
        }
        else
        {
            foreach (var filter in GenerateFiltersForRule(rule))
            {
                yield return filter;
            }
        }
    }

    private bool IsWildcardRule(string rule)
    {
        return rule == "*";
    }

    private bool IsNegatedRule(string rule)
    {
        return rule.StartsWith('#');
    }

    private IEnumerable<Func<string, bool>> ProcessNegatedRule(string rule)
    {
        var parts = rule.Split('&', StringSplitOptions.RemoveEmptyEntries)
                         .Select(part => part.TrimStart('#'));
        foreach (var part in parts)
        {
            foreach (var filter in GenerateFiltersForRule(part.Trim()))
            {
                yield return filter;
            }
        }
    }

    private IEnumerable<Func<string, bool>> GenerateFiltersForRule(string rule)
    {
        var conditions = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var condition in conditions)
        {
            string trimmedCondition = condition.Trim().ToLower();

            foreach (var handler in _conditionHandlers)
            {
                if (handler.Key(trimmedCondition))
                {
                    foreach (var filter in handler.Value(trimmedCondition))
                    {
                        yield return filter;
                    }
                    break;
                }
            }
        }
    }

    private IEnumerable<Func<string, bool>> GenerateIncludeExcludeFilter(string rule)
    {
        var parts = rule.Split('/');
        if (parts.Length != 2) yield break; // 如果规则不合法则跳过

        string includePattern = parts[0].Trim();
        string excludePattern = parts[1].Trim();

        yield return filePath =>
        {
            string fileName = Path.GetFileName(filePath).ToLower();
            return Regex.IsMatch(fileName, "^" + Regex.Escape(includePattern).Replace(@"\*", ".*") + "$") &&
                   !Regex.IsMatch(fileName, "^" + Regex.Escape(excludePattern).Replace(@"\*", ".*") + "$");
        };
    }

    private IEnumerable<Func<string, bool>> GenerateContainsKeywordFilter(string rule)
    {
        string keyword = rule.Trim('*');
        yield return filePath => Path.GetFileName(filePath).Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<Func<string, bool>> GenerateStartsWithPrefixFilter(string rule)
    {
        string prefix = rule.TrimEnd('*').ToLower();
        yield return filePath =>
        {
            string normalizedPath = Path.GetFileName(filePath).ToLower().Trim();
            return normalizedPath.StartsWith(prefix);
        };
    }

    private IEnumerable<Func<string, bool>> GenerateExtensionFilter(string rule)
    {
        string extension = rule.TrimStart('*').ToLower();
        yield return filePath => Path.GetExtension(filePath).ToLower() == extension;
    }

    private IEnumerable<Func<string, bool>> MatchesStartsWithAndEndsWith(string condition)
    {
        string prefix = condition.Split('*')[0].ToLower();
        string suffix = condition.TrimStart('*').ToLower();  // 后缀部分

        Func<string, bool> filter = filePath =>
        {
            string fileName = Path.GetFileName(filePath).ToLower();
            return fileName.StartsWith(prefix) && fileName.EndsWith(suffix);
        };

        return new Func<string, bool>[] { filter };
    }
}
