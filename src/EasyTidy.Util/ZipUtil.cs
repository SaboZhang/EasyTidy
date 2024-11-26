using EasyTidy.Model;
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
        using var archive = ZipFile.OpenRead(zipFilePath);
        foreach (var entry in archive.Entries)
        {
            // 过滤文件扩展名
            if (!string.IsNullOrWhiteSpace(filterExtension) && !entry.FullName.EndsWith(filterExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(entry.FullName) || entry.FullName.EndsWith("/"))
                continue;

            string relativePath = entry.FullName.Substring(rootFolderName.Length).TrimStart('/');
            string destinationPath = Path.Combine(directoryPath, relativePath);

            FileResolver.HandleFileConflict(entry.FullName, destinationPath, FileOperationType.Override, () =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                entry.ExtractToFile(destinationPath, true);
            });
        }
    }

    /// <summary>
    /// 处理提取到指定名称文件夹的逻辑。
    /// </summary>
    public static void ExtractToNamedFolder(string zipFilePath, string directoryPath, string folderName, string filterExtension = null)
    {
        string finalExtractPath = Path.Combine(directoryPath, folderName);
        DecompressToDirectory(zipFilePath, finalExtractPath, filterExtension);
    }

    /// <summary>
    /// 解压到目标目录。
    /// </summary>
    // public static bool DecompressToDirectory(string zipFilePath, string extractPath)
    // {
    //     try
    //     {
    //         using var archive = ZipFile.OpenRead(zipFilePath);
    //         foreach (var entry in archive.Entries)
    //         {
    //             if (string.IsNullOrWhiteSpace(entry.FullName)) continue;

    //             string destinationPath = Path.Combine(extractPath, entry.FullName);

    //             if (entry.FullName.EndsWith("/"))
    //             {
    //                 // 创建目录
    //                 Directory.CreateDirectory(destinationPath);
    //             }
    //             else
    //             {
    //                 HandleFileConflict(entry.FullName, destinationPath, FileOperationType.Overwrite, () =>
    //                 {
    //                     Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
    //                     entry.ExtractToFile(destinationPath, true);
    //                 });
    //             }
    //         }

    //         return true;
    //     }
    //     catch (Exception ex)
    //     {
    //         Debug.WriteLine("[FileExtractor] DecompressToDirectory Error: {0}", ex);
    //         return false;
    //     }
    // }

}
