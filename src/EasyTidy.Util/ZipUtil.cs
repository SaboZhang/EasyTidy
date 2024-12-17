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

namespace EasyTidy.Util;

public class ZipUtil
{
    public static void CompressFile(string filePath, string zipFilePath)
    {
        string[] fileExtensions = { ".json", ".db" };
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);
        foreach (var fileExtension in fileExtensions)
        {
            string[] filesToCompress = Directory.GetFiles(filePath, $"*{fileExtension}");

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
            foreach (var filePath in filesToCompress)
            {
                // 计算相对路径
                string relativePath = Path.GetRelativePath(sourcePath, filePath);
                string destinationFilePath = Path.Combine(tempDirectory, relativePath);

                // 确保目标目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));

                // 复制文件到临时目录
                File.Copy(filePath, destinationFilePath, overwrite: true);
            }

            // 创建压缩包
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

    public static bool DecompressToDirectory(string zipFilePath, string extractPath, string filterExtension = null)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipFilePath);
            foreach (var entry in archive.Entries)
            {
                // 过滤文件扩展名
                if (!string.IsNullOrWhiteSpace(filterExtension) && !entry.FullName.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // 过滤文件扩展名
                if (!string.IsNullOrWhiteSpace(filterExtension) && !entry.FullName.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
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

            if (string.IsNullOrWhiteSpace(entry.FullName) || entry.FullName.EndsWith("/"))
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
    public static void EncryptFolderToZip(string folderPath, string outputZipPath, string password)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException("指定的文件夹不存在");

        // 创建临时无密码的 ZIP 文件
        string tempZipPath = Path.GetTempFileName();

        try
        {
            // 打包文件夹到无密码的 ZIP 文件
            ZipFile.CreateFromDirectory(folderPath, tempZipPath);

            // 为 ZIP 文件加密
            EncryptZip(tempZipPath, outputZipPath, password);
        }
        finally
        {
            // 删除临时文件
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }
    }

    private static void EncryptZip(string inputZipPath, string outputZipPath, string password)
    {
        using (var inputFileStream = new FileStream(inputZipPath, FileMode.Open, FileAccess.Read))
        using (var outputFileStream = new FileStream(outputZipPath, FileMode.Create, FileAccess.Write))
        using (var aes = System.Security.Cryptography.Aes.Create())
        {
            aes.Key = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            aes.IV = new byte[16]; // 默认初始化向量为 0（与大多数工具兼容）

            // 写入加密的文件内容
            using (var cryptoStream = new System.Security.Cryptography.CryptoStream(
                outputFileStream,
                aes.CreateEncryptor(),
                System.Security.Cryptography.CryptoStreamMode.Write))
            {
                inputFileStream.CopyTo(cryptoStream);
            }
        }
    }

}
