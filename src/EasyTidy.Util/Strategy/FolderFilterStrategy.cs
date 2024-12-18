using System;
using System.Collections.Generic;
using System.IO;
using EasyTidy.Util.UtilInterface;

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

        // 分割多个条件
        string[] conditions = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

        // 遍历条件并生成匹配规则
        foreach (var condition in conditions)
        {
            string normalizedCondition = condition.Trim();
            yield return GenerateSingleFilter(normalizedCondition);
        }
    }

    private bool IsWildcardRule(string rule)
    {
        return rule == "**";
    }

    private static Func<string, bool> GenerateSingleFilter(string condition)
    {
        var handlers = new List<(Func<string, bool> IsMatch, Func<string, Func<string, bool>> CreateFilter)>
        {
            // **/ 排除规则
            (cond => cond.StartsWith("**/"), cond =>
            {
                string excludeKeyword = cond.Substring(3);
                return folderPath => Directory.Exists(folderPath) && !Path.GetFileName(folderPath).Contains(excludeKeyword);
            }),
            // ** 包含规则
            (cond => cond.StartsWith("**"), cond =>
            {
                string keyword = cond.Substring(2);
                return folderPath => Directory.Exists(folderPath) && Path.GetFileName(folderPath).Contains(keyword);
            }),
            // ** 后缀匹配规则
            (cond => cond.EndsWith("**"), cond =>
            {
                string prefix = cond.Substring(0, cond.Length - 2);
                return folderPath => Directory.Exists(folderPath) && Path.GetFileName(folderPath).StartsWith(prefix);
            }),
            // 默认精确匹配规则
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

        // 如果没有匹配规则，默认返回一个不匹配的过滤器（可以根据需求修改）
        return _ => false;
    }
}
