using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
        // 如果规则是 "*"，则返回一个文件存在检查器
        if (rule == "*")
        {
            yield return filePath => File.Exists(filePath);
        }
        else if (rule.StartsWith('#'))
        {
            // 处理包含 # 的规则，反转所有条件
            var parts = rule.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.TrimStart('#'));
            foreach (var part in parts)
            {
                foreach (var filter in GenerateFiltersForRule(part.Trim()))
                {
                    // 反转每个过滤器
                    yield return filePath => !filter(filePath);
                }
            }
        }
        else
        {
            // 处理不包含 # 的普通规则
            foreach (var filter in GenerateFiltersForRule(rule))
            {
                yield return filter;
            }
        }
    }

    private static IEnumerable<Func<string, bool>> GenerateFiltersForRule(string rule)
    {
        var conditions = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var condition in conditions)
        {
            string trimmedCondition = condition.Trim().ToLower();

            if (trimmedCondition.Contains('/'))
            {
                // 包含斜杠的规则，处理包含-排除模式
                foreach (var filter in GenerateIncludeExcludeFilter(trimmedCondition))
                {
                    yield return filter;
                }
            }
            else if (trimmedCondition.StartsWith("*"))
            {
                // 扩展名匹配规则
                yield return GenerateExtensionFilter(trimmedCondition);
            }
            else
            {
                // 默认规则，处理其他可能的条件
                yield return filePath => false; // 无匹配规则的情况
            }
        }
    }

    private static IEnumerable<Func<string, bool>> GenerateIncludeExcludeFilter(string condition)
    {
        var parts = condition.Split('/');
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

    private static Func<string, bool> GenerateExtensionFilter(string condition)
    {
        string requiredExtension = condition.StartsWith("*.") ? condition.Substring(1) : condition;

        return filePath =>
        {
            string fileExtension = Path.GetExtension(filePath).ToLower();
            // 使用小写扩展名进行匹配
            return fileExtension == requiredExtension;
        };
    }

    private static IEnumerable<Func<string, bool>> GenerateFolderFilters(string rule)
    {
        // 处理 ** 的情况，匹配全部文件夹
        if (rule == "**")
        {
            yield return folderPath => Directory.Exists(folderPath);
            yield break;
        }

        // 分割多个条件
        string[] conditions = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var condition in conditions)
        {
            string normalizedCondition = condition.Trim();

            // ** 开头，表示包含的匹配
            if (normalizedCondition.StartsWith("**/"))
            {
                string excludeKeyword = normalizedCondition.Substring(3); // 去掉前缀 **/
                yield return folderPath =>
                {
                    string folderName = Path.GetFileName(folderPath);
                    return Directory.Exists(folderPath) && !folderName.Contains(excludeKeyword);
                };
            }
            else if (normalizedCondition.StartsWith("**"))
            {
                string keyword = normalizedCondition.Substring(2); // 去掉前缀 **
                yield return folderPath =>
                {
                    string folderName = Path.GetFileName(folderPath);
                    return Directory.Exists(folderPath) && folderName.Contains(keyword);
                };
            }
            // 处理指定前缀的情况（如 reboot**）
            else if (normalizedCondition.EndsWith("**"))
            {
                string prefix = normalizedCondition.Substring(0, normalizedCondition.Length - 2); // 去掉后缀 **
                yield return folderPath =>
                {
                    string folderName = Path.GetFileName(folderPath);
                    return Directory.Exists(folderPath) && folderName.StartsWith(prefix);
                };
            }
            else // 处理其他特定规则
            {
                yield return folderPath =>
                {
                    string folderName = Path.GetFileName(folderPath);
                    return Directory.Exists(folderPath) && folderName == normalizedCondition;
                };
            }
        }
    }

    private static IEnumerable<Func<string, bool>> GenerateCustomFilters(string rule)
    {
        var filters = new List<Func<string, bool>>();

        if (rule.Contains(';') || rule.Contains('|'))
        {
            string[] parts = rule.Split([';', '|'], StringSplitOptions.RemoveEmptyEntries);
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

    public static bool ContainsTwoConsecutiveChars(string input, char character)
    {
        var pattern = new string(character, 2); // 创建一个包含两个指定字符的字符串
        return input.Contains(pattern); // 检查字符串是否包含这个模式
    }

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
            .Select(ext => ext.TrimStart('*').Trim())
            .ToList();

        // 用于存储非压缩文件后缀
        List<string> nonCompressedExtensions = new List<string>();

        // 检查每个后缀
        foreach (var ext in extensionList)
        {
            if (!compressedExtensions.Contains(ext))
            {
                // 如果不是压缩文件后缀，添加到 nonCompressedExtensions 列表
                nonCompressedExtensions.Add(ext);
            }
        }

        // 将非压缩文件后缀用分号连接成一个字符串返回
        return string.Join(";", nonCompressedExtensions);
    }

}
