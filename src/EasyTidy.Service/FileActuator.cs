using EasyTidy.Log;
using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Service;

public static class FileActuator
{
    
    public static void ExecuteFileOperation(OperationMode operationMode, string sourcePath, string targetPath, FileOperationType fileOperationType, List<Func<string, bool>> pathFilters = null)
    {
        if (Directory.Exists(sourcePath))
        {
            // 处理文件夹操作
            ProcessDirectory(sourcePath, targetPath, operationMode, fileOperationType, pathFilters);
        }
        else if (File.Exists(sourcePath))
        {
            // 处理单个文件操作
            ProcessFile(sourcePath, targetPath, operationMode, fileOperationType, pathFilters);
        }
        else
        {
            throw new FileNotFoundException($"Source path '{sourcePath}' not found.");
        }
    }

    private static void ProcessDirectory(string sourceDir, string targetDir, OperationMode operationMode, FileOperationType fileOperationType, List<Func<string, bool>> pathFilters = null)
    {
        // 创建目标目录
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // 获取所有文件
        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            if (ShouldSkip(pathFilters, filePath))
            {
                continue; // 跳过不符合条件的文件
            }
            string targetFilePath = Path.Combine(targetDir, Path.GetFileName(filePath));
            ProcessFile(filePath, targetFilePath, operationMode, fileOperationType, pathFilters);
        }

        // 递归处理子文件夹
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            if (ShouldSkip(pathFilters, subDir))
            {
                continue; // 跳过不符合条件的文件夹
            }
            string targetSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
            ProcessDirectory(subDir, targetSubDir, operationMode, fileOperationType, pathFilters);
        }
    }

    private static void ProcessFile(string sourcePath, string targetPath, OperationMode operationMode, FileOperationType fileOperationType, List<Func<string, bool>> pathFilters = null)
    {
        if (ShouldSkip(pathFilters, sourcePath))
        {
            return; // 跳过不符合条件的文件
        }
        switch (operationMode)
        {
            case OperationMode.Move:
                MoveFile(sourcePath, targetPath, fileOperationType);
                break;
            case OperationMode.Copy:
                CopyFile(sourcePath, targetPath, fileOperationType);
                break;
            case OperationMode.Delete:
                DeleteFile(sourcePath);
                break;
            case OperationMode.Rename:
                RenameFile(sourcePath, targetPath);
                break;
            case OperationMode.RecycleBin:
                MoveToRecycleBin(sourcePath);
                break;
            default:
                throw new NotSupportedException($"Operation mode '{operationMode}' is not supported.");
        }
    }

    private static void MoveFile(string sourcePath, string targetPath, FileOperationType fileOperationType)
    {
        HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
        {
            File.Move(sourcePath, targetPath);
        });
    }

    private static void CopyFile(string sourcePath, string targetPath, FileOperationType fileOperationType)
    {
        HandleFileConflict(sourcePath, targetPath, fileOperationType, () =>
        {
            File.Copy(sourcePath, targetPath);
        });
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void RenameFile(string sourcePath, string targetPath)
    {
        File.Move(sourcePath, targetPath);
    }

    private static void MoveToRecycleBin(string path)
    {
        // 需要使用一个外部库，例如 System.IO.FileSystem.Primitives
        // 这里用伪代码表示将文件移动到回收站的逻辑
    }

    private static void HandleFileConflict(string sourcePath, string targetPath, FileOperationType fileOperationType, Action action)
    {
        if (File.Exists(targetPath))
        {
            switch (fileOperationType)
            {
                case FileOperationType.Skip:
                    // Do nothing
                    break;
                case FileOperationType.Override:
                    action(); // 执行操作
                    break;
                case FileOperationType.OverwriteIfNewer:
                    if (File.GetLastWriteTime(sourcePath) > File.GetLastWriteTime(targetPath))
                    {
                        action(); // 执行操作
                    }
                    break;
                case FileOperationType.OverrideIfSizesDiffer:
                    if (new FileInfo(sourcePath).Length != new FileInfo(targetPath).Length)
                    {
                        action(); // 执行操作
                    }
                    break;
                case FileOperationType.ReNameAppend:
                    string newPath = GetUniqueFilePath(targetPath);
                    File.Copy(sourcePath, newPath);
                    break;
                case FileOperationType.ReNameAddDate:
                    newPath = GetUniqueFilePathWithDate(targetPath);
                    File.Copy(sourcePath, newPath);
                    break;
                default:
                    throw new NotSupportedException($"File operation type '{fileOperationType}' is not supported.");
            }
        }
        else
        {
            action(); // 如果目标文件不存在，直接执行操作
        }
    }

    private static string GetUniqueFilePath(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        int count = 1;

        string newFilePath = path;
        while (File.Exists(newFilePath))
        {
            newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension}_{count}{extension}");
            count++;
        }

        return newFilePath;
    }

    private static string GetUniqueFilePathWithDate(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        string newFileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";

        return Path.Combine(directory, newFileName);
    }

    private static bool ShouldSkip(List<Func<string, bool>> pathFilters, string path)
    {
        if (pathFilters == null || !pathFilters.Any())
        {
            return false; // 如果没有过滤规则，默认不过滤
        }

        // 遍历所有规则，只要有一个规则匹配就跳过
        foreach (var filter in pathFilters)
        {
            if (filter(path))
            {
                return true; // 跳过
            }
        }

        return false; // 不跳过
    }

}
