using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util.Strategy;
using System;
using System.Collections.Generic;
using System.IO;
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

    /// <summary>
    /// 检查是否应该跳过
    /// </summary>
    /// <param name="dynamicFilters"></param>
    /// <param name="path"></param>
    /// <param name="pathFilter"></param>
    /// <returns></returns>
    public static bool ShouldSkip(List<Func<string, bool>> dynamicFilters, string path, Func<string, bool>? pathFilter)
    {
        // 检查是否是快捷方式
        if (Path.GetExtension(path).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return true; // 跳过快捷方式文件
        }

        // 规则1检查：dynamicFilters 列表中满足任意一个条件
        bool satisfiesDynamicFilters = dynamicFilters != null && dynamicFilters.Any(filter => filter(path));

        // 规则2检查：如果 pathFilter 不为 null，则它应返回 true
        bool satisfiesPathFilter = pathFilter == null || pathFilter(path);

        // 如果 pathFilter 为 null，仅根据 satisfiesDynamicFilters 的结果返回
        if (pathFilter == null)
        {
            LogService.Logger.Debug($"satisfiesDynamicFilters (no pathFilter): {satisfiesDynamicFilters}");
            return !satisfiesDynamicFilters;
        }

        // 如果 pathFilter 不为 null，要求 satisfiesDynamicFilters 和 satisfiesPathFilter 同时满足
        LogService.Logger.Debug($"satisfiesDynamicFilters: {satisfiesDynamicFilters}, satisfiesPathFilter: {satisfiesPathFilter}");
        return satisfiesDynamicFilters ^ satisfiesPathFilter;
    }

    /// <summary>
    /// 判断传入的是否压缩文件规则
    /// </summary>
    /// <param name="extensions"></param>
    /// <returns></returns>
    public static string CheckAndCollectNonCompressedExtensions(string extensions)
    {
        // 定义压缩文件的后缀列表
        HashSet<string> compressedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".z", ".lz", ".iso"
        };

        // 分割传入字符串，支持两种分隔符 ';' 和 '|'
        var extensionList = extensions
            .Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.TrimStart('*').Trim())  // 去掉前面的星号并去除空格
            .ToList();

        // 用于存储非压缩文件后缀
        List<string> nonCompressedExtensions = new List<string>();

        // 遍历每个扩展名
        foreach (var ext in extensionList)
        {
            if (!compressedExtensions.Contains(ext))
            {
                // 如果不是压缩文件后缀，添加到 nonCompressedExtensions 列表
                nonCompressedExtensions.Add(ext);
            }
        }

        var compressedWithWildcard = compressedExtensions.Select(ext => "*" + ext).ToList();

        // 拼接非压缩文件的扩展名，并将其转换为 "*.ext" 格式
        var nonCompressedWithWildcard = nonCompressedExtensions
            .Select(ext => "*" + (ext.StartsWith('.') ? ext : "." + ext))  // 确保以 '.' 开头
            .ToList();

        // 将非压缩扩展名和压缩扩展名拼接
        var combinedExtensions = compressedWithWildcard.Concat(new[] { "@" }).Concat(nonCompressedWithWildcard);

        // 返回格式为 "*.zip;*.exe" 的字符串
        return string.Join(";", combinedExtensions);
    }

    public static int ToUnixTimestamp(DateTime value)
    {
        if (value == default(DateTime) || value == DateTime.MinValue)
        {
            // 设置为当前时间的 Unix 时间戳，或返回一个固定值
            value = DateTime.Now;
        }
        return (int)Math.Truncate((value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
    }

}
