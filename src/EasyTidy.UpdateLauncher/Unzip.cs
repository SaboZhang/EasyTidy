using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.UpdateLauncher;

public class Unzip
{
    /// <summary>
    ///     忽略的文件列表
    /// </summary>
    private static readonly string[] IgnoreFiles = ["run.bat"];

    private static readonly string ExcludeDirectory = "EasyTidy/";

    public static bool ExtractZipFile(string zipPath, string extractPath)
    {
        try
        {
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractPath += Path.DirectorySeparatorChar;

            using var archive = ZipFile.OpenRead(zipPath);

            foreach (var entry in archive.Entries)
            {
                if (!IsIgnoreFile(entry.FullName)) 
                {
                    // 检查是否需要排除目标目录自身
                    if (IsExcludedDirectory(entry.FullName, ExcludeDirectory))
                        continue;

                    // 获取解压后的完整路径（去除目标目录自身）
                    var relativePath = entry.FullName;
                    if (!string.IsNullOrEmpty(ExcludeDirectory) && relativePath.StartsWith(ExcludeDirectory, StringComparison.Ordinal))
                    {
                        relativePath = relativePath.Substring(ExcludeDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                    }

                    // 确保相对路径非空
                    if (string.IsNullOrWhiteSpace(relativePath)) continue;

                    var destinationPath = Path.GetFullPath(Path.Combine(extractPath, relativePath));

                    if (!IsDir(destinationPath))
                    {
                        // 确保目标路径的目录存在
                        var dir = Path.GetDirectoryName(destinationPath);
                        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                        // 如果文件已存在，则先删除
                        if (File.Exists(destinationPath)) File.Delete(destinationPath);

                        Debug.WriteLine($"抽取文件：{destinationPath}");
                        entry.ExtractToFile(destinationPath);
                    }
                    else
                    {
                        // 如果是目录，确保目录存在
                        if (!Directory.Exists(destinationPath))
                            Directory.CreateDirectory(destinationPath);
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"解压失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     指示文件是否是忽略的
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private static bool IsIgnoreFile(string fileName)
    {
        return Array.IndexOf(IgnoreFiles, fileName) != -1;
    }

    /// <summary>
    ///     指示路径是否是目录
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static bool IsDir(string path)
    {
        return path.Last() == '\\';
    }

    // 判断是否为需要排除的目录自身
    private static bool IsExcludedDirectory(string entryPath, string excludeDirectory)
    {
        if (string.IsNullOrEmpty(excludeDirectory)) return false;

        // 确保目录以分隔符结束
        if (!excludeDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            excludeDirectory += Path.DirectorySeparatorChar;

        // 条目是需要排除的根目录自身
        return string.Equals(entryPath, excludeDirectory, StringComparison.Ordinal);
    }

}
