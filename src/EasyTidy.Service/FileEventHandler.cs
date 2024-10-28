using EasyTidy.Model;
using System;
using System.IO;

namespace EasyTidy.Service;

public class FileEventHandler
{
    private FileSystemWatcher _watcher;

    /// <summary>
    /// 监控文件变化
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="targetPath"></param>
    /// <param name="delaySeconds"></param>
    /// <param name="fileOperationType"></param>
    public static void MonitorFolder(OperationMode operationMode, string folderPath, string targetPath, int delaySeconds, FileOperationType fileOperationType, RuleModel filter)
    {
        if (_watcher != null) return; // 防止重复监控

        _watcher = new FileSystemWatcher
        {
            Path = folderPath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            Filter = "*.*"
        };

        _watcher.Created += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter));
        _watcher.Deleted += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter));
        _watcher.Changed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter));
        _watcher.Renamed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter));
        _watcher.EnableRaisingEvents = true;

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
                return filter.ContentOperator == ContentOperatorEnum.Contains
                    ? content.Contains(filter.ContentValue)
                    : !content.Contains(filter.ContentValue);
            });
        }

        return filters;
    }

    // Helper methods
    private static long ConvertSizeToBytes(string sizeValue, SizeUnit sizeUnit)
    {
        // Conversion logic based on size unit (KB, MB, etc.)
        long size = long.Parse(sizeValue);
        return size * (sizeUnit == SizeUnit.KB ? 1024 : sizeUnit == SizeUnit.MB ? 1024 * 1024 : 1);
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
        List<Func<string, bool>> filters = new List<Func<string, bool>>();

        switch (ruleType)
        {
            case RuleType.FileRule:
                // 文件规则处理
                if (rule == "#")
                {
                    filters.Add(filePath => File.Exists(filePath));
                }
                else if (rule.Contains(";") || rule.Contains("|"))
                {
                    string[] suffixes = rule.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                    filters.Add(filePath =>
                    {
                        string extension = Path.GetExtension(filePath);
                        return suffixes.Any(suffix => $"*{extension}" == suffix);
                    });
                }
                else if (rule.Contains("/") && rule.Contains("*"))
                {
                    var parts = rule.Split('/');
                    string includePattern = parts[0];
                    string excludePattern = parts[1];

                    filters.Add(filePath =>
                    {
                        string fileName = Path.GetFileName(filePath);
                        return Regex.IsMatch(fileName, includePattern.Replace("*", ".*")) &&
                               !Regex.IsMatch(fileName, excludePattern.Replace("*", ".*"));
                    });
                }
                else if (rule == "*")
                {
                    filters.Add(filePath => File.Exists(filePath));
                }
                else
                {
                    filters.Add(filePath => Path.GetExtension(filePath) == rule.TrimStart('*'));
                }
                break;

            case RuleType.FolderRule:
                // 文件夹规则处理
                if (rule == "##")
                {
                    filters.Add(filePath => Directory.Exists(filePath));
                }
                else if (rule == "**")
                {
                    filters.Add(filePath => Directory.Exists(filePath));
                }
                // else if (rule.Contains("/") && rule.Contains("**"))
                // {
                //     var parts = rule.Split('/');
                //     string includePattern = parts[0];
                //     string excludePattern = parts[1];

                //     filters.Add(filePath =>
                //     {
                //         string directoryName = Path.GetFileName(filePath);
                //         return Regex.IsMatch(directoryName, includePattern.Replace("**", ".*")) &&
                //                !directoryName.StartsWith(excludePattern.Replace("**", ""));
                //     });
                // }
                else if (rule.StartsWith("**/")) // 处理 **/my** 的排除规则
                {
                    string exclusion = rule.Substring(3); // 去掉前面的 **/
                    filters.Add(filePath =>
                    {
                        string directoryName = Path.GetFileName(filePath);
                        return !directoryName.StartsWith(exclusion) && Directory.Exists(filePath);
                    });
                }
                else
                {
                    filters.Add(filePath =>
                    {
                        string directoryName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? string.Empty);
                        return directoryName == rule;
                    });
                }
                break;

            case RuleType.ExpressionRules:
                // 正则表达式处理
                filters.Add(filePath => Regex.IsMatch(filePath, rule));
                break;
        }

        return filters;
    }

}
