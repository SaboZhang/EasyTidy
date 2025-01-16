using EasyTidy.Log;
using EasyTidy.Model;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Windows.UI.ViewManagement;

namespace EasyTidy.Util;

public class ZipUtil
{
    public static void CompressFile(string filePath, string zipFilePath)
    {
        string[] fileExtensions = { "AppConfig.json", "EasyTidy_back.db", "CommonApplicationData.json" };
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);
        foreach (var fileExtension in fileExtensions)
        {
            string[] filesToCompress = Directory.GetFiles(filePath, $"{fileExtension}");

            foreach (var path in filesToCompress)
            {
                string destinationFilePath = Path.Combine(tempDirectory, Path.GetFileName(path));
                File.Copy(path, destinationFilePath);
            }
        }

        ZipFile.CreateFromDirectory(tempDirectory, zipFilePath);

        Directory.Delete(tempDirectory, true);
    }

    /// <summary>
    /// 压缩指定文件类型。
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="zipFilePath"></param>
    /// <param name="fileExtensions"></param>
    public static void CompressFile(string sourcePath, string targetPath, List<string> filesToCompress)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetFileName(sourcePath));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // 获取目标压缩包中的文件列表
            var filesInZip = GetFilesInMatchingZips(targetPath, sourcePath);

            // 过滤待压缩的文件
            var filteredFiles = FilterFilesToCompress(filesToCompress, filesInZip, sourcePath);

            if (filteredFiles.Count == 0)
            {
                LogService.Logger.Warn("所有文件已在压缩包中，无需重复压缩。");
                return;
            }

            foreach (var filePath in filteredFiles)
            {
                // 计算相对路径
                string relativePath = Path.GetRelativePath(sourcePath, filePath);
                string destinationFilePath = Path.Combine(tempDirectory, relativePath);

                // 确保目标目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));

                // 复制文件到临时目录
                File.Copy(filePath, destinationFilePath, overwrite: true);
            }

            // 创建压缩包或更新现有压缩包
            //if (File.Exists(targetPath))
            //{
            //    using (var archive = ZipFile.Open(targetPath, ZipArchiveMode.Update))
            //    {
            //        foreach (var filePath in filteredFiles)
            //        {
            //            string relativePath = Path.GetRelativePath(sourcePath, filePath);
            //            archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Fastest);
            //        }
            //    }
            //}
            //else
            //{
            //    ZipFile.CreateFromDirectory(tempDirectory, targetPath, CompressionLevel.Fastest, true);
            //}
            if (File.Exists(targetPath))
            {
                targetPath = FileResolver.GetUniquePath(targetPath);
            }
            ZipFile.CreateFromDirectory(tempDirectory, targetPath, CompressionLevel.Fastest, true);
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"压缩文件时发生错误: {ex.Message}", ex);
            throw;
        }
        finally
        {
            // 清理临时目录
            Directory.Delete(tempDirectory, true);
        }
    }

    private static HashSet<string> GetFilesInMatchingZips(string zipFilePath, string sourcePath)
    {
        var matchingZipFiles = GetMatchingZipFiles(zipFilePath);
        var allFilesInZip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 遍历所有匹配的压缩包，提取每个压缩包中的文件列表
        foreach (var zipFile in matchingZipFiles)
        {
            var filesInZip = GetFilesInZip(zipFile, sourcePath);
            allFilesInZip.UnionWith(filesInZip); // 合并文件路径集合
        }

        return allFilesInZip;
    }

    public static bool DecompressToDirectory(string zipFilePath, string extractPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipFilePath);
            foreach (var entry in archive.Entries)
            {
                //如果目录不存在则创建
                if (!Directory.Exists(extractPath)) Directory.CreateDirectory(extractPath);
                if (entry.FullName[^1..] == "/")
                {
                    //如果为目录则继续创建该目录但不解压
                    Directory.CreateDirectory(Path.Combine(extractPath, entry.FullName));
                    continue;
                }

                var entryDestination = Path.Combine(extractPath, entry.FullName);
                entry.ExtractToFile(entryDestination, true);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[ZipUtil] DecompressToDirectory Error, {0}", ex);
            return false;
        }
    }

    /// <summary>
    /// 处理单一文件提取。
    /// </summary>
    public static void ExtractSingleFile(string zipFilePath, string directoryPath, string filterExtension = null)
    {
        using var archive = ZipFile.OpenRead(zipFilePath);
        var entry = archive.Entries.First();
        directoryPath = CreateFolderForFile(zipFilePath);

        string destinationPath = Path.Combine(directoryPath, entry.Name);
        FileResolver.HandleFileConflict(entry.FullName, destinationPath, FileOperationType.Override, () =>
        {
            // 过滤文件扩展名
            if (!string.IsNullOrWhiteSpace(filterExtension) && !entry.FullName.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            entry.ExtractToFile(destinationPath, overwrite: true);
        });
    }

    /// <summary>
    /// 处理单一文件夹提取。
    /// </summary>
    public static void ExtractSingleDirectory(string zipFilePath, string directoryPath, string rootFolderName, string filterExtension = null)
    {
        // 确保目标目录存在并创建根文件夹
        string targetDirectory = Path.Combine(directoryPath, rootFolderName);
        Directory.CreateDirectory(targetDirectory);

        // 获取文件扩展名
        string extension = Path.GetExtension(zipFilePath).ToLower();

        // 根据文件扩展名判断解压方式
        if (extension == ".zip")
        {
            ExtractZip(zipFilePath, targetDirectory, rootFolderName, filterExtension);
        }
        else if (extension == ".tar" || extension == ".gz" || extension == ".tgz" || extension == ".tar.gz")
        {
            ExtractTarGz(zipFilePath, targetDirectory, rootFolderName, filterExtension);
        }
        else if (extension == ".rar" || extension == ".7z")
        {
            ExtractRar7z(zipFilePath, targetDirectory, rootFolderName, filterExtension);
        }
        else
        {
            throw new NotSupportedException($"The file format '{extension}' is not supported.");
        }
    }

    private static void ExtractZip(string zipFilePath, string targetDirectory, string rootFolderName, string filterExtension)
    {
        using var archive = ZipFile.OpenRead(zipFilePath);
        foreach (var entry in archive.Entries)
        {
            if (!string.IsNullOrWhiteSpace(filterExtension) && !entry.FullName.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.FullName) || entry.FullName.EndsWith('/'))
                continue;

            string relativePath = entry.FullName.Substring(rootFolderName.Length).TrimStart('/');
            string destinationPath = Path.Combine(targetDirectory, relativePath);

            FileResolver.HandleFileConflict(entry.FullName, destinationPath, FileOperationType.Override, () =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                entry.ExtractToFile(destinationPath, true);
            });
        }
    }

    private static void ExtractTarGz(string filePath, string targetDirectory, string rootFolderName, string filterExtension)
    {
        using var archive = ArchiveFactory.Open(filePath);
        foreach (var entry in archive.Entries)
        {
            if (!entry.IsDirectory)
            {
                if (!string.IsNullOrWhiteSpace(filterExtension) && !entry.Key.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Key.EndsWith("/"))
                    continue;

                string relativePath = entry.Key.Substring(rootFolderName.Length).TrimStart('/');
                string destinationPath = Path.Combine(targetDirectory, relativePath);

                FileResolver.HandleFileConflict(entry.Key, destinationPath, FileOperationType.Override, () =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    entry.WriteToDirectory(targetDirectory);
                });
            }
        }
    }

    private static void ExtractRar7z(string filePath, string targetDirectory, string rootFolderName, string filterExtension)
    {
        var extensions = string.IsNullOrWhiteSpace(filterExtension) 
            ? Array.Empty<string>() 
            : filterExtension.Split([';','|']).Select(ext => ext.Trim()).ToArray();
        using var archive = ArchiveFactory.Open(filePath);
        var reader = archive.ExtractAllEntries();
        while (reader.MoveToNextEntry())
        {
            if (!reader.Entry.IsDirectory)
            {
                if (extensions.Any() && !extensions.Any(ext => reader.Entry.Key.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;  // 跳过不符合扩展名的文件
                }

                if (string.IsNullOrWhiteSpace(reader.Entry.Key) || reader.Entry.Key.EndsWith('/'))
                    continue;
                // 获取文件相对路径，并构建目标路径
                string relativePath = reader.Entry.Key;
                string destinationPath = Path.Combine(targetDirectory, relativePath);

                // 创建文件的目录结构（如果不存在）
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                // 将文件解压到目标路径，覆盖已存在文件
                reader.WriteEntryToFile(destinationPath, new ExtractionOptions() { Overwrite = true });
            }
        }
    }

    /// <summary>
    /// 处理提取到指定名称文件夹的逻辑。
    /// </summary>
    public static void ExtractToNamedFolder(string zipFilePath, string directoryPath, string folderName, string filterExtension = null)
    {
        // 获取文件扩展名
        string extension = Path.GetExtension(zipFilePath).ToLower();
        // 根据文件扩展名选择解压方式
        if (extension == ".zip")
        {
            ExtractZipToFolder(zipFilePath, directoryPath, folderName, filterExtension);
        }
        else if (extension == ".tar" || extension == ".gz" || extension == ".tgz" || extension == ".tar.gz")
        {
            ExtractTarGzToFolder(zipFilePath, directoryPath, folderName, filterExtension);
        }
        else if (extension == ".rar" || extension == ".7z")
        {
            ExtractRar7zToFolder(zipFilePath, directoryPath, folderName, filterExtension);
        }
        else
        {
            throw new NotSupportedException($"The file format '{extension}' is not supported for extraction.");
        }
    }

    private static void ExtractZipToFolder(string zipFilePath, string directoryPath, string folderName, string filterExtension)
    {
        string finalExtractPath = Path.Combine(directoryPath, folderName);
        Directory.CreateDirectory(finalExtractPath);

        using (var archive = ZipFile.OpenRead(zipFilePath))
        {
            foreach (var entry in archive.Entries)
            {
                if (!entry.FullName.EndsWith('/') && (string.IsNullOrWhiteSpace(filterExtension) || entry.FullName.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase)))
                {
                    string destinationPath = Path.Combine(finalExtractPath, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)); // Ensure the directory exists
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        }
    }

    private static void ExtractTarGzToFolder(string filePath, string directoryPath, string folderName, string filterExtension)
    {
        string finalExtractPath = Path.Combine(directoryPath, folderName);
        Directory.CreateDirectory(finalExtractPath);

        using (var archive = ArchiveFactory.Open(filePath))
        {
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory && (string.IsNullOrWhiteSpace(filterExtension) || entry.Key.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase)))
                {
                    string destinationPath = Path.Combine(finalExtractPath, entry.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)); // Ensure the directory exists
                    entry.WriteToDirectory(destinationPath);
                }
            }
        }
    }

    private static void ExtractRar7zToFolder(string filePath, string directoryPath, string folderName, string filterExtension)
    {
        // 创建最终的解压目录
        string finalExtractPath = Path.Combine(directoryPath, folderName);
        Directory.CreateDirectory(finalExtractPath);

        var extensions = string.IsNullOrWhiteSpace(filterExtension) 
            ? Array.Empty<string>() 
            : filterExtension.Split([';','|']).Select(ext => ext.Trim()).ToArray();

        using (var archive = ArchiveFactory.Open(filePath))
        {
            var reader = archive.ExtractAllEntries();
            // 如果目标解压路径不存在，则创建
            if (!Directory.Exists(finalExtractPath))
            {
                Directory.CreateDirectory(finalExtractPath);
            }

            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    if (extensions.Any() && !extensions.Any(ext => reader.Entry.Key.EndsWith(ext.TrimStart('*'), StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;  // 跳过不符合扩展名的文件
                    }

                    if (string.IsNullOrWhiteSpace(reader.Entry.Key) || reader.Entry.Key.EndsWith('/'))
                        continue;
                    // 获取文件相对路径，并构建目标路径
                    string relativePath = reader.Entry.Key;
                    string destinationPath = Path.Combine(finalExtractPath, relativePath);

                    // 创建文件的目录结构（如果不存在）
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                    // 将文件解压到目标路径，覆盖已存在文件
                    reader.WriteEntryToFile(destinationPath, new ExtractionOptions() { Overwrite = true });
                }
            }
        }
    }

    private static string CreateFolderForFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("The specified file does not exist.", filePath);

        // 获取文件所在目录
        string directoryPath = Path.GetDirectoryName(filePath);

        // 获取文件名（不包含扩展名）
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

        // 组合文件夹路径
        string folderPath = Path.Combine(directoryPath, fileNameWithoutExtension);

        // 创建文件夹（如果不存在）
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return folderPath;
    }

    /// <summary>
    /// 对压缩包加密
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="outputZipPath"></param>
    /// <param name="password"></param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// <summary>
    /// 使用 7-Zip 压缩文件夹并加密。
    /// </summary>
    /// <param name="folderPath">待压缩的文件夹路径。</param>
    /// <param name="outputZipPath">压缩后的输出文件路径。</param>
    /// <param name="password">压缩文件的密码。</param>
    public static void EncryptFolderToZip(string folderPath, string outputZipPath, string password)
    {
        ValidatePaths(folderPath, outputZipPath, password);

        // 从固定路径加载 7-Zip
        string sevenZipPath = Get7ZipPath();

        // 如果输出路径没有扩展名，默认添加 .7z
        if (string.IsNullOrWhiteSpace(Path.GetExtension(outputZipPath)))
        {
            outputZipPath += ".7z";
        }

        string arguments = Build7ZipArguments(folderPath, outputZipPath, password);

        Execute7ZipProcess(sevenZipPath, arguments, folderPath);
    }

    /// <summary>
    /// 验证路径合法性。
    /// </summary>
    private static void ValidatePaths(string folderPath, string outputZipPath, string password)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || (!Directory.Exists(folderPath) && !File.Exists(folderPath)))
            throw new ArgumentException("指定的文件或者文件夹不存在。");

        if (string.IsNullOrWhiteSpace(outputZipPath))
            throw new ArgumentException("输出压缩文件路径无效。", nameof(outputZipPath));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("密码不能为空。", nameof(password));
    }

    /// <summary>
    /// 获取 7-Zip 的路径。
    /// </summary>
    private static string Get7ZipPath()
    {
        string sevenZipPath = Path.Combine(Constants.ExecutePath, "Assets", "lib", "7z.exe");

        if (!File.Exists(sevenZipPath))
            throw new FileNotFoundException("7-Zip 可执行文件未找到，请检查路径是否正确。", sevenZipPath);

        return sevenZipPath;
    }

    /// <summary>
    /// 构建 7-Zip 命令参数。
    /// </summary>
    private static string Build7ZipArguments(string inputPath, string outputZipPath, string password)
    {
        // -a 添加文件到压缩包；-p 设置密码；-mhe 隐藏文件名（7z 格式支持）
        if (Directory.Exists(inputPath))
        {
            // 如果是文件夹，则压缩整个文件夹
            return $"a \"{outputZipPath}\" \"{inputPath}\\*\" -p\"{password}\" -mhe";
        }
        else if (File.Exists(inputPath))
        {
            // 如果是文件，则压缩单个文件
            return $"a \"{outputZipPath}\" \"{inputPath}\" -p\"{password}\" -mhe";
        }
        throw new InvalidOperationException("输入路径既不是文件夹也不是文件。");
    }

    /// <summary>
    /// 执行 7-Zip 命令行进程。
    /// </summary>
    private static void Execute7ZipProcess(string sevenZipPath, string arguments, string workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = sevenZipPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        try
        {
            using (var process = Process.Start(processStartInfo))
            {
                if (process == null)
                    throw new InvalidOperationException("无法启动 7-Zip 进程。");

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string errorOutput = process.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"7-Zip 压缩失败，错误信息：{errorOutput}");
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("执行 7-Zip 压缩过程中发生错误。", ex);
        }
    }

    private static HashSet<string> GetFilesInZip(string zipFilePath, string sourcePath)
    {
        var filesInZip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(zipFilePath))
        {
            using (var archive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (var entry in archive.Entries)
                {
                    // 计算相对路径，确保使用统一的路径分隔符（'/'）
                    string relativePath = entry.FullName.Replace('\\', '/');

                    // 获取 source 目录名并移除前缀
                    string sourceDirectoryName = Path.GetFileName(sourcePath).Replace('\\', '/');

                    // 如果压缩包中的路径以 source 目录名开头，则去掉前缀
                    if (relativePath.StartsWith(sourceDirectoryName, StringComparison.OrdinalIgnoreCase))
                    {
                        relativePath = relativePath.Substring(sourceDirectoryName.Length).TrimStart('/');
                    }

                    // 添加到 HashSet 中
                    filesInZip.Add(relativePath);
                }
            }
        }

        return filesInZip;
    }

    private static List<string> GetMatchingZipFiles(string zipFilePath)
    {
        // 提取文件名和目录路径
        string directoryPath = Path.GetDirectoryName(zipFilePath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(zipFilePath);
        string fileExtension = Path.GetExtension(zipFilePath);

        var matchingZipFiles = new List<string>();

        if (Directory.Exists(directoryPath))
        {
            // 获取目录中所有的 zip 文件
            var zipFiles = Directory.GetFiles(directoryPath, $"{fileNameWithoutExtension}*{fileExtension}");

            // 将匹配的文件路径添加到列表中
            matchingZipFiles.AddRange(zipFiles);
        }

        return matchingZipFiles;
    }

    private static List<string> FilterFilesToCompress(List<string> filesToCompress, HashSet<string> filesInZip, string sourcePath)
    {
        var filteredFiles = new List<string>();

        // 获取 source 目录名并移除前缀
        string sourceDirectoryName = Path.GetFileName(sourcePath).Replace('\\', '/');

        foreach (var filePath in filesToCompress)
        {
            // 计算相对路径
            string relativePath = Path.GetRelativePath(sourcePath, filePath).Replace('\\', '/');

            // 如果相对路径以 source 目录名开头，则移除该部分
            if (relativePath.StartsWith(sourceDirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(sourceDirectoryName.Length).TrimStart('/');
            }

            // 如果文件不在压缩包中，则加入待压缩列表
            if (!filesInZip.Contains(relativePath))
            {
                filteredFiles.Add(filePath);
            }
        }

        return filteredFiles;
    }

    /// <summary>
    /// 压缩文件，并设置密码
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="outputFilePath"></param>
    /// <param name="password"></param>
    public static void CompressWithPassword(string inputFilePath, string outputFilePath, string password)
    {
        ValidatePaths(inputFilePath, outputFilePath, password);

        string sevenZipPath = Get7ZipPath();

        if (!Path.GetExtension(outputFilePath).Equals(".7z", StringComparison.OrdinalIgnoreCase) 
            && !Path.GetExtension(outputFilePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            outputFilePath += ".7z";
        }

        string arguments = Build7ZipArguments(inputFilePath, outputFilePath, password);

        Execute7ZipProcess(sevenZipPath, arguments, "");
        if (CommonUtil.Configs.OriginalFile)
        {
            File.Delete(inputFilePath);
        }
    }

}
