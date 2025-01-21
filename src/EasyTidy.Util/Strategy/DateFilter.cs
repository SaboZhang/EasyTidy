using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System;
using System.IO;

namespace EasyTidy.Util.Strategy;

public class DateFilter : IFileFilter
{
    private readonly string _dateValue;
    private readonly DateUnit _dateUnit;
    private readonly ComparisonResult _dateOperator;
    private readonly DateType _dateType;

    public DateFilter(string dateValue, DateUnit dateUnit, ComparisonResult dateOperator, DateType dateType)
    {
        _dateValue = dateValue;
        _dateUnit = dateUnit;
        _dateOperator = dateOperator;
        _dateType = dateType;
    }

    public Func<string, bool> GenerateFilter(FilterTable filter)
    {
        return path =>
        {
            // 检查路径是否有效
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            DateTime entityDate = GetEntityDate(path, _dateType);

            // 转换日期范围或单个日期
            var result = FilterUtil.ConvertToDateTime(_dateValue, _dateUnit);
            if (result is Tuple<DateTime, DateTime> dateRange)
            {
                return FilterUtil.CompareDates(entityDate, _dateOperator, dateRange.Item1, dateRange.Item2);
            }
            else if (result is DateTime singleDate)
            {
                return FilterUtil.CompareDates(entityDate, _dateOperator, singleDate);
            }

            // 默认返回 false（防止意外情况）
            return false;
        };
    }

    // 根据路径类型获取日期
    private DateTime GetEntityDate(string path, DateType dateType)
    {
        if (File.Exists(path))
        {
            return dateType switch
            {
                DateType.Create => File.GetCreationTime(path),
                DateType.Edit => File.GetLastWriteTime(path),
                DateType.Visit => File.GetLastAccessTime(path),
                _ => DateTime.Now
            };
        }
        else if (Directory.Exists(path))
        {
            var directoryInfo = new DirectoryInfo(path);
            return dateType switch
            {
                DateType.Create => directoryInfo.CreationTime,
                DateType.Edit => directoryInfo.LastWriteTime,
                DateType.Visit => directoryInfo.LastAccessTime,
                _ => DateTime.Now
            };
        }
        else
        {
            throw new FileNotFoundException($"The path '{path}' does not exist as a file or directory.");
        }
    }

}
