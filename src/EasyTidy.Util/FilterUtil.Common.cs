using EasyTidy.Log;
using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    internal static (long? FirstSize, long? SecondSize) ConvertSizeToBytes(string sizeValue, SizeUnit sizeUnit)
    {
        // 转换逻辑，基于大小单位（字节、KB、MB、GB等）
        // 分割 sizeValue，以逗号为分隔符
        var sizes = sizeValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .ToArray();

        // 校验并解析每个值
        long ConvertToBytes(string size) => long.Parse(size) * sizeUnit switch
        {
            SizeUnit.Kilobyte => 1024,
            SizeUnit.Megabyte => 1024 * 1024,
            SizeUnit.Gigabyte => 1024 * 1024 * 1024,
            SizeUnit.Byte => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(sizeUnit), "Invalid size unit.")
        };

        // 处理单个值或两个值的情况
        return sizes.Length switch
        {
            1 => (ConvertToBytes(sizes[0]), null), // 单个值，返回第一个值，第二个值为 null
            2 => (ConvertToBytes(sizes[0]), ConvertToBytes(sizes[1])), // 两个值，返回元组
            _ => throw new ArgumentException("sizeValue must contain at most two comma-separated values.", nameof(sizeValue))
        };
    }

    /// <summary>
    /// 日期转换
    /// </summary>
    /// <param name="dateValue"></param>
    /// <param name="dateUnit"></param>
    /// <returns></returns>
    internal static object ConvertToDateTime(string dateValue, DateUnit dateUnit)
    {
        // Parse and adjust date based on unit (days, months, etc.)
        // 解析传入的字符串
        var dateParts = dateValue.Split(',');

        if (dateParts.Length == 2)
        {
            // 对两个日期分别进行处理
            DateTime date1 = ConvertDate(dateParts[0], dateUnit);
            DateTime date2 = ConvertDate(dateParts[1], dateUnit);

            return (date1, date2); // 返回元组
        }
        else if (dateParts.Length == 1)
        {
            // 仅处理一个日期
            DateTime date = ConvertDate(dateParts[0], dateUnit);
            return date; // 返回单个日期
        }

        // 如果传入的字符串不符合预期，返回当前时间
        return DateTime.Now;
    }

    private static DateTime ConvertDate(string dateValue, DateUnit dateUnit)
    {
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
    internal static bool CompareValues(long fileValue, long? filterValue, long? filterValueTwo, ComparisonResult comparison)
    {
        return comparison switch
        {
            ComparisonResult.GreaterThan => fileValue > filterValue,
            ComparisonResult.LessThan => fileValue < filterValue,
            ComparisonResult.Equal => fileValue == filterValue,
            ComparisonResult.Between => fileValue >= filterValue && fileValue <= filterValueTwo,
            ComparisonResult.NotBetween => fileValue < filterValue || fileValue > filterValueTwo,
            _ => false,
        };
    }

    /// <summary>
    /// 比较日期
    /// </summary>
    /// <param name="fileDate">文件日期</param>
    /// <param name="filterDate">指定的时间单位转换之后的日期</param>
    /// <param name="comparison">比较方式</param>
    /// <returns></returns>
    internal static bool CompareDates(DateTime fileDate, ComparisonResult comparison, params DateTime[] filterDates)
    {
        if (filterDates.Length == 0)
        {
            return false;
        }
        return comparison switch
        {
            ComparisonResult.GreaterThan => filterDates.All(filterDate => filterDate > fileDate),
            ComparisonResult.LessThan => filterDates.All(filterDate => filterDate < fileDate),
            ComparisonResult.Equal => filterDates.All(filterDate => fileDate == filterDate),
            ComparisonResult.Between when filterDates.Length == 2 => filterDates[0] >= fileDate && filterDates[1] <= fileDate,
            ComparisonResult.NotBetween when filterDates.Length == 2 => filterDates[0] < fileDate || filterDates[1] > fileDate, // 不在两个日期之间
            _ => false,
        };
    }

    public static Dictionary<string, string> ExtractKeyValuePairsFromRaw(string raw)
    {
        try
        {
            if (string.IsNullOrEmpty(raw))
            {
                return new Dictionary<string, string>();
            }

            // Step 1: Remove code block symbols and clean the JSON string
            string cleanedJson = Regex.Replace(raw, @"```(?:json|c#)?\s*([\s\S]*?)```", "$1").Trim();

            // 如果没有找到代码块，尝试直接解析原始内容
            if (string.IsNullOrEmpty(cleanedJson))
            {
                cleanedJson = raw.Trim();
            }

            // 尝试查找第一个 { 和最后一个 } 之间的内容
            int startIndex = cleanedJson.IndexOf('{');
            int endIndex = cleanedJson.LastIndexOf('}');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                cleanedJson = cleanedJson.Substring(startIndex, endIndex - startIndex + 1);
            }
            else
            {
                LogService.Logger.Warn($"无法找到有效的AI输出内容: {cleanedJson}");
                return new Dictionary<string, string>();
            }

            // Step 2: Parse as JSON with options
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };

            // Step 3: Parse JSON and extract key-value pairs
            var result = new Dictionary<string, string>();
            using var doc = JsonDocument.Parse(cleanedJson, options);
            ExtractRecursive(doc.RootElement, result, "");
            return result;
        }
        catch (JsonException ex)
        {
            LogService.Logger.Error($"JSON解析错误: {ex.Message}, 原始内容: {raw}");
            return new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"提取键值对时发生错误: {ex.Message}");
            return new Dictionary<string, string>();
        }
    }

    private static void ExtractRecursive(JsonElement element, Dictionary<string, string> dict, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    string fullKey = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    ExtractRecursive(prop.Value, dict, fullKey);
                }
                break;

            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    string arrayKey = $"{prefix}[{index}]";
                    ExtractRecursive(item, dict, arrayKey);
                    index++;
                }
                break;

            default:
                dict[prefix] = element.ToString();
                break;
        }
    }
}
