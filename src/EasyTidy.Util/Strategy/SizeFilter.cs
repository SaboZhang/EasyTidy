using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Util.Strategy;

public class SizeFilter : IFileFilter
{
    private readonly string _sizeValue;
    private readonly SizeUnit _sizeUnit;
    private readonly ComparisonResult _sizeOperator;

    public SizeFilter(string sizeValue, SizeUnit sizeUnit, ComparisonResult sizeOperator)
    {
        _sizeValue = sizeValue;
        _sizeUnit = sizeUnit;
        _sizeOperator = sizeOperator;
    }

    public Func<string, bool> GenerateFilter(FilterTable filter)
    {
        // 检查路径并返回是否符合过滤条件
        return path =>
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            if (File.Exists(path))
            {
                return IsFileMatch(path, filter);
            }

            if (Directory.Exists(path))
            {
                return IsDirectoryMatch(path, filter);
            }

            // 非文件或文件夹的路径直接返回 false
            return false;
        };
    }

    // 检查文件是否符合过滤条件
    private bool IsFileMatch(string filePath, FilterTable filter)
    {
        var fileInfo = new FileInfo(filePath);
        long fileSize = fileInfo.Length;

        var (minSize, maxSize) = FilterUtil.ConvertSizeToBytes(filter.SizeValue, filter.SizeUnit);
        return FilterUtil.CompareValues(fileSize, minSize, maxSize, filter.SizeOperator);
    }

    // 检查文件夹是否符合过滤条件
    private bool IsDirectoryMatch(string directoryPath, FilterTable filter)
    {
        var directoryInfo = new DirectoryInfo(directoryPath);
        long folderSize = CalculateDirectorySize(directoryInfo);
        LogService.Logger.Debug($"文件夹大小{folderSize}");

        var (minSize, maxSize) = FilterUtil.ConvertSizeToBytes(filter.SizeValue, filter.SizeUnit);
        return FilterUtil.CompareValues(folderSize, minSize, maxSize, filter.SizeOperator);
    }

    // 计算文件夹的总大小（包括子文件夹中的所有文件）
    private long CalculateDirectorySize(DirectoryInfo directoryInfo)
    {
        try
        {
            return directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                                .Sum(file => file.Length);
        }
        catch (UnauthorizedAccessException)
        {
            // 忽略无权限访问的文件或文件夹
            return 0;
        }
        catch (Exception ex)
        {
            // 记录异常日志（假设有日志工具）
            LogService.Logger.Error($"Failed to calculate directory size for {directoryInfo.FullName}: {ex.Message}");
            return 0;
        }
    }

}
