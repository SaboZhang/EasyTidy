using EasyTidy.Model;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EasyTidy.Service;

public static class FileEventHandler
{
    private static Dictionary<string, FileSystemWatcher> _watchers = [];
    private static readonly char[] separator = [';', '|'];

    /// <summary>
    /// 监控文件变化
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="targetPath"></param>
    /// <param name="delaySeconds"></param>
    /// <param name="fileOperationType"></param>
    public static void MonitorFolder(OperationMode operationMode, string folderPath, string targetPath, int delaySeconds, RuleModel filter, FileOperationType fileOperationType = FileOperationType.Skip)
    {
        if (_watchers.ContainsKey(folderPath)) return; // 防止重复监控

        var watcher = new FileSystemWatcher
        {
            Path = folderPath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            Filter = "*.*"
        };

        watcher.Created += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter));
        watcher.Deleted += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter));
        watcher.Changed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter));
        watcher.Renamed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter));
        watcher.EnableRaisingEvents = true;

        _watchers[folderPath] = watcher; // 存储监控器
    }

    private static void OnFileChange(FileSystemEventArgs e, int delaySeconds, Action action)
    {
        System.Threading.ThreadPool.QueueUserWorkItem(state =>
        {
            System.Threading.Thread.Sleep(delaySeconds * 1000);
            action();
        });
    }

    private static void HandleFileChange(OperationMode operationMode, string source, string target, FileOperationType fileOperationType, RuleModel filter)
    {
        // 定义过滤规则
        List<Func<string, bool>> pathFilters = GetPathFilters(filter.Filter);
        // 根据 rule 和 RuleType 动态生成的过滤条件
        List<Func<string, bool>> dynamicFilters = GeneratePathFilters(filter.Rule, filter.RuleType);
        // 合并两组过滤条件
        pathFilters.AddRange(dynamicFilters);
        // FileActuator.OnExecuteMoveFile(source, target, fileOperationType);FileActuator.OnExecuteMoveFile(source, target, fileOperationType);
        FileActuator.ExecuteFileOperation(operationMode, source, target, fileOperationType, pathFilters);
    }

    private static List<Func<string, bool>> GetPathFilters(FilterTable filter)
    {
        List<Func<string, bool>> filters = [];

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
        
        return filters;
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

    private static List<Func<string, bool>> GeneratePathFilters(string rule, TaskRuleType ruleType)
    {
        List<Func<string, bool>> filters = [];

        switch (ruleType)
        {
            case TaskRuleType.FileRule:
                filters.AddRange(GenerateFileFilters(rule));
                break;

            case TaskRuleType.FolderRule:
                filters.AddRange(GenerateFolderFilters(rule));
                break;

            case TaskRuleType.CustomRule:
                filters.AddRange(GenerateCustomFilters(rule));
                break;
            case TaskRuleType.ExpressionRules:
                filters.Add(filePath => Regex.IsMatch(filePath, rule));
                break;
        }

        return filters;
    }

    private static IEnumerable<Func<string, bool>> GenerateFileFilters(string rule)
    {
        if (rule == "#")
        {
            yield return filePath => File.Exists(filePath);
        }
        else
        {
            string[] conditions = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var condition in conditions)
            {
                if (condition.Contains("/"))
                {
                    var parts = condition.Split('/');
                    string includePattern = parts[0];
                    string excludePattern = parts[1];

                    yield return filePath =>
                    {
                        string fileName = Path.GetFileName(filePath);
                        return Regex.IsMatch(fileName, includePattern.Replace("*", ".*")) &&
                               !Regex.IsMatch(fileName, excludePattern.Replace("*", ".*"));
                    };
                }
                else
                {
                    string normalizedCondition = condition.Trim();
                    if (normalizedCondition == "*")
                    {
                        yield return filePath => File.Exists(filePath);
                    }
                    else
                    {
                        string extension = Path.GetExtension(normalizedCondition);
                        yield return filePath => Path.GetExtension(filePath) == extension;
                    }
                }
            }
        }
    }

    private static IEnumerable<Func<string, bool>> GenerateFolderFilters(string rule)
    {
        if (rule == "##")
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
                if (part.StartsWith("file:"))
                {
                    filters.AddRange(GenerateFileFilters(part.Substring(5)));
                }
                else if (part.StartsWith("folder:"))
                {
                    filters.AddRange(GenerateFolderFilters(part.Substring(7)));
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

    public static void StopAllMonitoring()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
    }

}
