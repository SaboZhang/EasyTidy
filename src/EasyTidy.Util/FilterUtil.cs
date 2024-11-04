using EasyTidy.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EasyTidy.Util;

public class FilterUtil
{
    public static Func<string, bool>? GetPathFilters(FilterTable filter)
    {

        if (filter == null)
        {
            return null; // 没有过滤器，接受所有
        }

        List<Func<string, bool>> filters = new List<Func<string, bool>>();

        // Size Filter
        if (filter.IsSizeSelected)
        {
            filters.Add(filePath =>
            {
                long fileSize = new FileInfo(filePath).Length;
                long filterSize = ConvertSizeToBytes(filter.SizeValue, filter.SizeUnit);

                return CompareValues(fileSize, filterSize, filter.SizeOperator);
            });
        }

        // Create Date Filter
        if (filter.IsCreateDateSelected)
        {
            filters.Add(filePath =>
            {
                DateTime creationDate = File.GetCreationTime(filePath);
                DateTime filterDate = ConvertToDateTime(filter.CreateDateValue, filter.CreateDateUnit);

                return CompareDates(creationDate, filterDate, filter.CreateDateOperator);
            });
        }

        // Edit Date Filter
        if (filter.IsEditDateSelected)
        {
            filters.Add(filePath =>
            {
                DateTime editDate = File.GetLastWriteTime(filePath);
                DateTime filterDate = ConvertToDateTime(filter.EditDateValue, filter.EditDateUnit);

                return CompareDates(editDate, filterDate, filter.EditDateOperator);
            });
        }

        // Visit Date Filter
        if (filter.IsVisitDateSelected)
        {
            filters.Add(filePath =>
            {
                DateTime visitDate = File.GetLastAccessTime(filePath);
                DateTime filterDate = ConvertToDateTime(filter.VisitDateValue, filter.VisitDateUnit);

                return CompareDates(visitDate, filterDate, filter.VisitDateOperator);
            });
        }

        // Archive Filter
        if (filter.IsArchiveSelected)
        {
            filters.Add(filePath => new FileInfo(filePath).Attributes.HasFlag(FileAttributes.Archive) == (filter.ArchiveValue == YesOrNo.Yes));
        }

        // Hidden Filter
        if (filter.IsHiddenSelected)
        {
            filters.Add(filePath => new FileInfo(filePath).Attributes.HasFlag(FileAttributes.Hidden) == (filter.HiddenValue == YesOrNo.Yes));
        }

        // ReadOnly Filter
        if (filter.IsReadOnlySelected)
        {
            filters.Add(filePath => new FileInfo(filePath).IsReadOnly == (filter.ReadOnlyValue == YesOrNo.Yes));
        }

        // System Filter
        if (filter.IsSystemSelected)
        {
            filters.Add(filePath => new FileInfo(filePath).Attributes.HasFlag(FileAttributes.System) == (filter.SystemValue == YesOrNo.Yes));
        }

        // Temporary Filter
        if (filter.IsTempSelected)
        {
            filters.Add(filePath => new FileInfo(filePath).Attributes.HasFlag(FileAttributes.Temporary) == (filter.TempValue == YesOrNo.Yes));
        }

        // Included Files Filter
        if (filter.IsIncludeSelected)
        {
            filters.Add(filePath => filter.IncludedFiles.Split(',').Contains(Path.GetFileName(filePath)));
        }

        // Content Filter (Example: for text files only)
        if (filter.IsContentSelected)
        {
            filters.Add(filePath =>
            {
                string content = File.ReadAllText(filePath);
                string contentValue = filter.ContentValue;

                return filter.ContentOperator switch
                {
                    ContentOperatorEnum.AtLeastOneWord =>
                        content.Split(' ').Any(word => word.Equals(contentValue, StringComparison.OrdinalIgnoreCase)),

                    ContentOperatorEnum.AtLeastOneWordCaseSensitive =>
                        content.Split(' ').Any(word => word.Equals(contentValue, StringComparison.Ordinal)),

                    ContentOperatorEnum.AllWordsInAnyOrder =>
                        contentValue.Split(' ').All(word => content.Contains(word, StringComparison.OrdinalIgnoreCase)),

                    ContentOperatorEnum.AllWordsInAnyOrderCaseSensitive =>
                        contentValue.Split(' ').All(word => content.Contains(word, StringComparison.Ordinal)),

                    ContentOperatorEnum.RegularExpression =>
                        Regex.IsMatch(content, contentValue),

                    ContentOperatorEnum.String =>
                        content.Contains(contentValue, StringComparison.OrdinalIgnoreCase),

                    ContentOperatorEnum.StringCaseSensitive =>
                        content.Contains(contentValue, StringComparison.Ordinal),

                    _ => false
                };
            });
        }

        return filePath => filters.All(f => f(filePath));
    }

    // Helper methods
    private static long ConvertSizeToBytes(string sizeValue, SizeUnit sizeUnit)
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

    private static DateTime ConvertToDateTime(string dateValue, DateUnit dateUnit)
    {
        // Parse and adjust date based on unit (days, months, etc.)
        DateTime date = DateTime.Parse(dateValue);
        return date;
    }

    private static bool CompareValues(long fileValue, long filterValue, ComparisonResult comparison)
    {
        return comparison switch
        {
            ComparisonResult.GreaterThan => fileValue > filterValue,
            ComparisonResult.LessThan => fileValue < filterValue,
            ComparisonResult.Equal => fileValue == filterValue,
            _ => false,
        };
    }

    private static bool CompareDates(DateTime fileDate, DateTime filterDate, ComparisonResult comparison)
    {
        return comparison switch
        {
            ComparisonResult.GreaterThan => fileDate > filterDate,
            ComparisonResult.LessThan => fileDate < filterDate,
            ComparisonResult.Equal => fileDate == filterDate,
            _ => false,
        };
    }

    public static List<Func<string, bool>> GeneratePathFilters(string rule, TaskRuleType ruleType)
    {
        var filters = new List<Func<string, bool>>();

        switch (ruleType)
        {
            case TaskRuleType.FileRule:
                foreach (var filter in GenerateFileFilters(rule))
                {
                    filters.Add(filter);
                }
                break;

            case TaskRuleType.FolderRule:
                foreach (var filter in GenerateFolderFilters(rule))
                {
                    filters.Add(filter);
                }
                break;

            case TaskRuleType.CustomRule:
                foreach (var filter in GenerateCustomFilters(rule))
                {
                    filters.Add(filter);
                }
                break;
            case TaskRuleType.ExpressionRules:
                filters.Add(filePath => Regex.IsMatch(filePath, rule));
                break;
        }

        return filters;
    }


    public static IEnumerable<Func<string, bool>> GenerateFileFilters(string rule)
    {
        // 检查是否为“处理所有文件”的规则
        if (rule == "*")
        {
            yield return filePath => File.Exists(filePath);
        }
        else
        {
            // 分割多个条件（后缀规则）
            string[] conditions = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 为每个条件创建过滤器
            foreach (var condition in conditions)
            {
                string trimmedCondition = condition.Trim().ToLower();

                if (trimmedCondition.Contains('/'))
                {
                    // 包含斜杠处理包含-排除模式
                    var parts = trimmedCondition.Split('/');
                    string includePattern = parts[0].Trim();
                    string excludePattern = parts[1].Trim();

                    yield return filePath =>
                    {
                        string fileName = Path.GetFileName(filePath).ToLower();
                        return Regex.IsMatch(fileName, "^" + Regex.Escape(includePattern).Replace(@"\*", ".*") + "$") &&
                               !Regex.IsMatch(fileName, "^" + Regex.Escape(excludePattern).Replace(@"\*", ".*") + "$");
                    };
                }
                else if (trimmedCondition.StartsWith("*."))
                {
                    // 处理常见的扩展名匹配，确保严格匹配指定的扩展名
                    string requiredExtension = trimmedCondition.Substring(1); // 去掉 "*"

                    yield return filePath =>
                    {
                        // 确保前面加上点，并进行匹配
                        string fileExtension = Path.GetExtension(filePath).ToLower();
                        return fileExtension == requiredExtension; // 确保前面加上点
                    };
                }
            }
        }
    }


    private static IEnumerable<Func<string, bool>> GenerateFolderFilters(string rule)
    {
        if (rule == "**")
        {
            yield return folderPath => Directory.Exists(folderPath);
        }
        else
        {
            string[] conditions = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var condition in conditions)
            {
                string normalizedCondition = condition.Trim();
                if (normalizedCondition == "**")
                {
                    yield return folderPath => Directory.Exists(folderPath);
                }
                else
                {
                    yield return folderPath =>
                    {
                        string folderName = Path.GetFileName(folderPath);
                        return folderName == normalizedCondition.Replace("**", "");
                    };
                }
            }
        }
    }

    private static IEnumerable<Func<string, bool>> GenerateCustomFilters(string rule)
    {
        var filters = new List<Func<string, bool>>();

        if (rule.Contains(';') || rule.Contains('|'))
        {
            string[] parts = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (ContainsTwoConsecutiveChars(part, '*') || ContainsTwoConsecutiveChars(part, '#'))
                {
                    filters.AddRange(GenerateFolderFilters(part));
                }
                else
                {
                    filters.AddRange(GenerateFileFilters(part));
                }
            }
        }
        else
        {
            // 处理单个规则
            filters.AddRange(GenerateFileFilters(rule));
            filters.AddRange(GenerateFolderFilters(rule));
        }

        return filters;
    }

    private static bool ContainsTwoConsecutiveChars(string input, char character)
    {
        var pattern = new string(character, 2); // 创建一个包含两个指定字符的字符串
        return input.Contains(pattern); // 检查字符串是否包含这个模式
    }

}
