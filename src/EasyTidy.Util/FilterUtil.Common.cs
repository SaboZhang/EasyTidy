using EasyTidy.Log;
using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasyTidy.Util;

public partial class FilterUtil
{
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
