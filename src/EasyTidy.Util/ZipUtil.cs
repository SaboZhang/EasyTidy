using EasyTidy.Model;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
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
    public static void CompressFile(string filePath, string zipFilePath, string[] fileExtensions)
    {
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
                    entry.WriteToDirectory(destinationPath);
                });
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
        // string finalExtractPath = Path.Combine(directoryPath, folderName);

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
        // DecompressToDirectory(zipFilePath, finalExtractPath, filterExtension);
    }

    private static void ExtractZipToFolder(string zipFilePath, string directoryPath, string folderName, string filterExtension)
    {
        string finalExtractPath = Path.Combine(directoryPath, folderName);
        Directory.CreateDirectory(finalExtractPath);

        using (var archive = ZipFile.OpenRead(zipFilePath))
        {
            foreach (var entry in archive.Entries)
            {
                if (!entry.FullName.EndsWith("/") && (string.IsNullOrWhiteSpace(filterExtension) || entry.FullName.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase)))
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

        using (var archive = ArchiveFactory.Open(filePath))
        {
            // 如果目标解压路径不存在，则创建
            if (!Directory.Exists(finalExtractPath))
            {
                Directory.CreateDirectory(finalExtractPath);
            }

            foreach (var entry in archive.Entries)
            {
                // 过滤文件扩展名
                if (!string.IsNullOrWhiteSpace(filterExtension) && !entry.Key.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 如果是目录，则创建目录
                if (entry.IsDirectory)
                {
                    string dirPath = Path.Combine(finalExtractPath, entry.Key);
                    Directory.CreateDirectory(dirPath);
                    continue;
                }

                // 解压文件
                // string entryDestination = Path.Combine(finalExtractPath, entry.Key);

                // 确保父目录存在
                string directory = Path.GetDirectoryName(finalExtractPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 解压文件到目标路径
                entry.WriteToDirectory(finalExtractPath, new ExtractionOptions { ExtractFullPath = true, Overwrite = true});
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

}
