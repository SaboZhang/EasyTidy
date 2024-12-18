using EasyTidy.Log;
using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasyTidy.Util;

public partial class FilterUtil
{
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

    internal static bool IsFolderRule(string rule)
    {
        // 判断规则是否为文件夹规则，规则包含 "**" 或 "##" 时视为文件夹规则
        return ContainsTwoConsecutiveChars(rule, '*') || ContainsTwoConsecutiveChars(rule, '#');
    }

    /// <summary>
    ///  文件大小单位转换
    /// </summary>
    /// <param name="sizeValue"></param>
    /// <param name="sizeUnit"></param>
    /// <returns></returns>
    internal static long ConvertSizeToBytes(string sizeValue, SizeUnit sizeUnit)
    {
        // 转换逻辑，基于大小单位（字节、KB、MB、GB等）
        long size = long.Parse(sizeValue);

        return size * (sizeUnit switch
        {
            SizeUnit.Kilobyte => 1024,
            SizeUnit.Megabyte => 1024 * 1024,
            SizeUnit.Gigabyte => 1024 * 1024 * 1024,
            SizeUnit.Byte => 1,
            _ => 1 // 默认情况下返回字节
        });
    }

    /// <summary>
    /// 日期转换
    /// </summary>
    /// <param name="dateValue"></param>
    /// <param name="dateUnit"></param>
    /// <returns></returns>
    internal static DateTime ConvertToDateTime(string dateValue, DateUnit dateUnit)
    {
        // Parse and adjust date based on unit (days, months, etc.)
        if (int.TryParse(dateValue, out int numericValue))
        {
            return dateUnit switch
            {
                DateUnit.Day => DateTime.Now.AddDays(-numericValue),
                DateUnit.Month => DateTime.Now.AddMonths(-numericValue),
                DateUnit.Year => DateTime.Now.AddYears(-numericValue),
                DateUnit.Second => DateTime.Now.AddSeconds(-numericValue),
                DateUnit.Minute => DateTime.Now.AddMinutes(-numericValue),
                DateUnit.Hour => DateTime.Now.AddHours(-numericValue),
                _ => DateTime.Now,
            };
        }

        return DateTime.Now;
    }

    /// <summary>
    /// 比较文件大小和日期
    /// </summary>
    /// <param name="fileValue"></param>
    /// <param name="filterValue"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    internal static bool CompareValues(long fileValue, long filterValue, ComparisonResult comparison)
    {
        return comparison switch
        {
            ComparisonResult.GreaterThan => fileValue > filterValue,
            ComparisonResult.LessThan => fileValue < filterValue,
            ComparisonResult.Equal => fileValue == filterValue,
            _ => false,
        };
    }

    /// <summary>
    /// 比较日期
    /// </summary>
    /// <param name="fileDate"></param>
    /// <param name="filterDate"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    internal static bool CompareDates(DateTime fileDate, DateTime filterDate, ComparisonResult comparison)
    {
        return comparison switch
        {
            ComparisonResult.GreaterThan => filterDate > fileDate,
            ComparisonResult.LessThan => filterDate < fileDate,
            ComparisonResult.Equal => fileDate == filterDate,
            _ => false,
        };
    }
}
