using EasyTidy.Model;
using SharpCompress.Archives;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace EasyTidy.Util;

public class FileResolver
{
    private static readonly string[] SupportedArchiveExtensions = { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".z", ".lz", ".iso" };

    /// <summary>
    /// 处理文件冲突
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="fileOperationType"></param>
    /// <param name="action"></param>
    /// <param name="isMove"></param>
    /// <param name="inUse"></param>
    /// <exception cref="NotSupportedException"></exception>
    public static void HandleFileConflict(string sourcePath, string targetPath, FileOperationType fileOperationType, Action action, bool isMove = false, bool inUse = false)
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
                    if (File.GetLastWriteTime(sourcePath) > File.GetLastWriteTime(targetPath)
                        || Directory.GetLastWriteTime(sourcePath) > Directory.GetLastWriteTime(targetPath))
                    {
                        action(); // 执行操作
                    }
                    break;
                case FileOperationType.OverrideIfSizesDiffer:
                    if (AreFileSizesEqual(sourcePath, targetPath))
                    {
                        action(); // 执行操作
                    }
                    break;
                case FileOperationType.ReNameAppend:
                    ProcessPath(sourcePath, targetPath, isMove, inUse, false);
                    break;
                case FileOperationType.ReNameAddDate:
                    ProcessPath(sourcePath, targetPath, isMove, inUse, true);
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

    /// <summary>
    /// 检查两个文件夹的大小是否相等
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <returns></returns>
    private static bool AreFileSizesEqual(string sourcePath, string targetPath)
    {
        if (File.Exists(sourcePath) && File.Exists(targetPath))
        {
            // 比较文件大小
            return new FileInfo(sourcePath).Length != new FileInfo(targetPath).Length;
        }
        else if (Directory.Exists(sourcePath) && Directory.Exists(targetPath))
        {
            // 如果是文件夹，调用自定义方法来比较文件夹大小
            return GetDirectorySize(sourcePath) != GetDirectorySize(targetPath);
        }
        else
        {
            // LogService.Logger.Error("无效文件夹或者文件路径");
            return false;
        }
    }

    /// <summary>
    /// 获取唯一路径
    /// </summary>
    /// <param name="path"></param>
    /// <param name="withDate"></param>
    /// <returns></returns>
    public static string GetUniquePath(string path, bool withDate = false)
    {
        if (File.Exists(path) || Directory.Exists(path))
        {
            return withDate ? GetUniquePathWithDate(path) : GetUniquePathWithoutDate(path);
        }
        return path;
    }

    // 无时间戳的唯一路径生成器
    private static string GetUniquePathWithoutDate(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        int count = 1;

        string newPath = path;

        if (File.Exists(path)) // 如果是文件
        {
            while (File.Exists(newPath))
            {
                newPath = Path.Combine(directory, $"{name}_{count++}{extension}");
            }
        }
        else if (Directory.Exists(path)) // 如果是文件夹
        {
            while (Directory.Exists(newPath))
            {
                newPath = Path.Combine(directory, $"{name}_{count++}");
            }
        }

        return newPath;
    }

    // 带时间戳的唯一路径生成器
    private static string GetUniquePathWithDate(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");

        string newPath;
        if (File.Exists(path)) // 如果是文件
        {
            newPath = Path.Combine(directory, $"{name}_{timestamp}{extension}");
        }
        else // 如果是文件夹
        {
            newPath = Path.Combine(directory, $"{name}_{timestamp}");
        }

        return newPath;
    }

    /// <summary>
    /// 获取文件夹的大小
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private static long GetDirectorySize(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

        return Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                        .Sum(file => new FileInfo(file).Length);
    }

    /// <summary>
    /// 复制整个文件夹
    /// </summary>
    /// <param name="sourceDir"></param>
    /// <param name="destDir"></param>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDir, filePath);
            string destFilePath = Path.Combine(destDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
            File.Copy(filePath, destFilePath, true);
        }
    }

    /// <summary>
    /// 处理路径
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="isMove"></param>
    /// <param name="inUse"></param>
    /// <param name="withData"></param>
    private static void ProcessPath(string sourcePath, string targetPath, bool isMove, bool inUse, bool withData)
    {
        string newPath = GetUniquePath(targetPath, withData);

        if (File.Exists(sourcePath))
        {
            // 如果是文件，则执行文件的移动或复制
            if (isMove)
            {
                File.Move(sourcePath, newPath);
            }
            else
            {
                File.Copy(sourcePath, newPath);
            }
        }
        else if (Directory.Exists(sourcePath))
        {
            // 如果是文件夹，则执行文件夹的移动或复制
            if (isMove)
            {
                Directory.Move(sourcePath, newPath);
            }
            else
            {
                CopyDirectory(sourcePath, newPath);
            }
        }
    }

    /// <summary>
    /// 判断扩展名是否为压缩文件格式。
    /// </summary>
    public static bool IsArchiveFile(string extension)
    {
        return SupportedArchiveExtensions.Contains(extension);
    }

    /// <summary>
    /// 分析压缩包内容。
    /// </summary>
    public static (bool isSingleFile, bool isSingleDirectory, string rootFolderName) AnalyzeCompressedContent(string compressedFilePath)
    {
        // 获取文件扩展名，判断文件类型
        string extension = Path.GetExtension(compressedFilePath).ToLower();

        // 根据文件扩展名调用不同的解压分析方法
        if (extension == ".zip")
        {
            return AnalyzeZipContent(compressedFilePath);
        }
        else if (extension == ".tar" || extension == ".gz" || extension == ".tgz" || extension == ".tar.gz")
        {
            return AnalyzeTarGzContent(compressedFilePath);
        }
        else if (extension == ".rar" || extension == ".7z")
        {
            return AnalyzeRar7zContent(compressedFilePath);
        }
        else
        {
            throw new NotSupportedException($"The file format '{extension}' is not supported for analysis.");
        }
    }

    private static (bool isSingleFile, bool isSingleDirectory, string rootFolderName) AnalyzeZipContent(string zipFilePath)
    {
        using var archive = ZipFile.OpenRead(zipFilePath);
        var entries = archive.Entries;

        var rootFolders = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.FullName))
            .Select(e => e.FullName.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .Distinct()
            .ToList();

        if (entries.Count == 1 && !entries.First().FullName.EndsWith("/"))
        {
            // 单个文件
            return (true, false, null);
        }
        else if (rootFolders.Count == 1)
        {
            // 单个文件夹
            return (false, true, rootFolders.First());
        }

        return (false, false, null);
    }

    private static (bool isSingleFile, bool isSingleDirectory, string rootFolderName) AnalyzeTarGzContent(string filePath)
    {
        using var archive = ArchiveFactory.Open(filePath);
        var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();

        var rootFolders = entries
            .Select(e => e.Key.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .Distinct()
            .ToList();

        if (entries.Count == 1)
        {
            // 单个文件
            return (true, false, null);
        }
        else if (rootFolders.Count == 1)
        {
            // 单个文件夹
            return (false, true, rootFolders.First());
        }

        return (false, false, null);
    }

    private static (bool isSingleFile, bool isSingleDirectory, string rootFolderName) AnalyzeRar7zContent(string filePath)
    {
        using var archive = ArchiveFactory.Open(filePath);
        var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();

        var rootFolders = entries
            .Select(e => e.Key.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .Distinct()
            .ToList();

        if (entries.Count == 1)
        {
            // 单个文件
            return (true, false, null);
        }
        else if (rootFolders.Count == 1)
        {
            // 单个文件夹
            return (false, true, rootFolders.First());
        }

        return (false, false, null);
    }

    public static void CopyAllFiles(string sourceFolder, string destinationFolder, bool overwrite = true)
    {
        if (string.IsNullOrEmpty(sourceFolder) || string.IsNullOrEmpty(destinationFolder))
        {
            throw new ArgumentException("Source and destination folders cannot be null or empty.");
        }

        // 确保源文件夹存在
        if (!Directory.Exists(sourceFolder))
        {
            throw new DirectoryNotFoundException($"Source folder does not exist: {sourceFolder}");
        }

        // 确保目标文件夹存在
        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        // 获取源文件夹中的所有文件
        var files = Directory.GetFiles(sourceFolder);

        foreach (var file in files)
        {
            // 获取文件名
            var fileName = Path.GetFileName(file);

            // 构建目标文件路径
            var destFile = Path.Combine(destinationFolder, fileName);

            // 复制文件
            File.Copy(file, destFile, overwrite);
        }
    }

    public static FileSystemNode GetDirectorySnapshot(string directoryPath, OperationParameters parameters)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");

        return ScanDirectory(directoryPath, parameters);
    }

    private static FileSystemNode ScanDirectory(string directoryPath, OperationParameters parameters)
    {
        var root = new FileSystemNode
        {
            Name = Path.GetFileName(directoryPath),
            Path = directoryPath,
            IsFolder = true,
            FolderCount = 0, // 初始化为0
            FileCount = 0,
            Size = 0
        };

        try
        {
            // 遍历子文件夹
            foreach (var dir in Directory.GetDirectories(directoryPath))
            {
                HandleSubdirectory(root, dir, parameters);
            }

            // 遍历文件
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                HandleFile(root, file, parameters);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            // 捕获根目录级别的访问异常（可选日志）
            Console.WriteLine($"无法访问目录: {directoryPath}. 错误: {ex.Message}");
        }

        return root;
    }

    private static void HandleSubdirectory(FileSystemNode root, string dir, OperationParameters parameters)
    {
        try
        {
            var dirInfo = new DirectoryInfo(dir);

            if (ShouldSkipFile(dirInfo.Attributes) || ShouldSkipFolder(parameters, dir))
                return;

            var childNode = ScanDirectory(dir, parameters);
            root.Children.Add(childNode);

            // 更新当前目录的计数和大小
            root.FolderCount++;
            root.FileCount += childNode.FileCount;
            root.Size += childNode.Size ?? 0;
        }
        catch (Exception ex)
        {
            // 捕获子文件夹访问异常（可选日志）
            Console.WriteLine($"无法访问文件夹: {dir}. 错误: {ex.Message}");
        }
    }

    private static void HandleFile(FileSystemNode root, string file, OperationParameters parameters)
    {
        try
        {
            var fileInfo = new FileInfo(file);

            if (ShouldSkipFile(fileInfo.Attributes) ||
                (!"*".Equals(parameters.RuleModel.Rule) && FilterUtil.ShouldSkip(parameters.Funcs, file, parameters.PathFilter)))
            {
                return;
            }

            root.Children.Add(new FileSystemNode
            {
                Name = fileInfo.Name,
                Path = fileInfo.FullName,
                IsFolder = false,
                Size = fileInfo.Length,
                ModifiedDate = GetFileModifiedDate(fileInfo)
            });

            // 更新当前目录的文件计数和大小
            root.FileCount++;
            root.Size += fileInfo.Length;
        }
        catch (Exception ex)
        {
            // 捕获文件访问异常（可选日志）
            Console.WriteLine($"无法访问文件: {file}. 错误: {ex.Message}");
        }
    }

    private static DateTime GetFileModifiedDate(FileInfo fileInfo)
    {
        return fileInfo.LastWriteTime != DateTime.MinValue
            ? fileInfo.LastWriteTime
            : fileInfo.CreationTime;
    }

    private static bool ShouldSkipFile(FileAttributes attributes)
    {
        bool isSystemFile = (attributes & FileAttributes.System) != 0;
        bool isHiddenFile = (attributes & FileAttributes.Hidden) != 0;

        if (((bool)CommonUtil.Configs.GeneralConfig.IrrelevantFiles && isSystemFile) ||
            ((bool)CommonUtil.Configs.GeneralConfig.HiddenFiles && isHiddenFile))
        {
            return true;
        }

        return false;
    }

    public static bool ShouldSkipFolder(OperationParameters parameters, string dir)
    {
        if ("**".Equals(parameters.RuleModel.Rule) || "*".Equals(parameters.RuleModel.Rule))
        {
            return false;
        }
        return (FilterUtil.ShouldSkip(parameters.Funcs, dir, parameters.PathFilter)
            && parameters.RuleModel.RuleType == TaskRuleType.FolderRule)
            || FilterUtil.ContainsTwoConsecutiveChars(parameters.RuleModel.Rule, '*')
            || FilterUtil.ContainsTwoConsecutiveChars(parameters.RuleModel.Rule, '#');
    }

}
