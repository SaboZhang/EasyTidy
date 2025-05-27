using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace EasyTidy.Util.Strategy;

public class FolderFilterStrategy : IFilterStrategy
{
    public IEnumerable<Func<string, bool>> GenerateFilters(string rule)
    {
        if (IsWildcardRule(rule))
        {
            yield return folderPath => Directory.Exists(folderPath);
            yield break;
        }

        string[] conditions = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var condition in conditions)
        {
            string normalizedCondition = condition.Trim();
            yield return GenerateSingleFilter(normalizedCondition);
        }
    }

    private static bool IsWildcardRule(string rule)
    {
        return rule == "**";
    }

    public IEnumerable<FilterItem> GenerateFilterItems(string rule)
    {
        if (IsWildcardRule(rule))
        {
            yield return new FilterItem
            {
                Predicate = folderPath => Directory.Exists(folderPath),
                IsExclude = false
            };
            yield break;
        }

        string[] conditions = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var condition in conditions)
        {
            string normalized = condition.Trim();
            bool isExclude = normalized.StartsWith("**/");
            string cleaned = isExclude ? normalized.Substring(3) : normalized;

            yield return new FilterItem
            {
                Predicate = GenerateSingleFilter(normalized),
                IsExclude = isExclude
            };
        }
    }

    private static Func<string, bool> GenerateSingleFilter(string condition)
    {
        var handlers = new List<(Func<string, bool> IsMatch, Func<string, Func<string, bool>> CreateFilter)>
        {
            (cond => cond.StartsWith("**/"), cond =>
            {
                string excludeKeyword = cond.Substring(3).TrimEnd('*');
                Debug.WriteLine($"Exclude keyword: {excludeKeyword}");
                return folderPath => Directory.Exists(folderPath) && Path.GetFileName(folderPath).Contains(excludeKeyword);
            }),
            (cond => cond.StartsWith("**"), cond =>
            {
                string keyword = cond.Substring(2);
                return folderPath => Directory.Exists(folderPath) && Path.GetFileName(folderPath).Contains(keyword);
            }),
            (cond => cond.EndsWith("**"), cond =>
            {
                string prefix = cond.Substring(0, cond.Length - 2);
                return folderPath => Directory.Exists(folderPath) && Path.GetFileName(folderPath).StartsWith(prefix);
            }),
            (cond => true, cond =>
            {
                return folderPath => Directory.Exists(folderPath) && Path.GetFileName(folderPath) == cond;
            })
        };

        foreach (var (isMatch, createFilter) in handlers)
        {
            if (isMatch(condition))
            {
                return createFilter(condition);
            }
        }

        return _ => false;
    }
}
