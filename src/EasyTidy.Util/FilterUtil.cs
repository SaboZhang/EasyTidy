using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyTidy.Util;

public partial class FilterUtil
{
    /// <summary>
    /// 获取文件属性过滤器
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static Func<string, bool>? GetPathFilters(FilterTable filter)
    {
        if (filter == null)
        {
            return null; // 没有过滤器，接受所有
        }

        var filters = FilterFactory.GetFilters(filter);

        // 返回所有过滤器的组合
        return filePath => filters.All(f => f.GenerateFilter(filter)(filePath));
    }

    /// <summary>
    /// 根据规则类型获取对应过滤器
    /// </summary>
    /// <param name="rule"></param>
    /// <param name="ruleType"></param>
    /// <returns></returns>
    public static List<Func<string, bool>> GeneratePathFilters(string rule, TaskRuleType ruleType)
    {
        var filters = new List<Func<string, bool>>();
        // 策略模式
        var filterFunctions = new Dictionary<TaskRuleType, Func<string, IEnumerable<Func<string, bool>>>>()
        {
            { TaskRuleType.FileRule, rule => new FileFilterStrategy().GenerateFilters(rule) },
            { TaskRuleType.FolderRule, rule => new FolderFilterStrategy().GenerateFilters(rule) },
            { TaskRuleType.CustomRule, rule => new CustomFilterStrategy().GenerateFilters(rule) },
            { TaskRuleType.ExpressionRules, rule => new ExpressionFilterStrategy().GenerateFilters(rule) }
        };

        if (filterFunctions.TryGetValue(ruleType, out var generateFilters))
        {
            foreach (var filter in generateFilters(rule))
            {
                filters.Add(filter);
            }
        }
        else
        {
            LogService.Logger.Error($"{ruleType} is not supported.");
            return filters;
        }

        return filters;
    }

    /// <summary>
    /// 检查字符串是否包含两个连续的字符
    /// </summary>
    /// <param name="input"></param>
    /// <param name="character"></param>
    /// <returns></returns>
    public static bool ContainsTwoConsecutiveChars(string input, char character)
    {
        var pattern = new string(character, 2); // 创建一个包含两个指定字符的字符串
        return input.Contains(pattern); // 检查字符串是否包含这个模式
    }

}
