using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasyTidy.Util.Strategy;

public class IncludeFilter : IFileFilter
{
    private readonly HashSet<string> _includedFiles;

    public IncludeFilter(string includedFiles)
    {
        // 将文件名列表拆分并存储在 HashSet 中，保证查找的效率
        _includedFiles = new HashSet<string>(includedFiles.Split(',').Select(fileName => fileName.Trim()), StringComparer.OrdinalIgnoreCase);
    }

    public Func<string, bool> GenerateFilter(FilterTable filter)
    {
        return filePath =>
        {
            // 获取文件名，确保文件名匹配的大小写不敏感
            string fileName = Path.GetFileName(filePath);
            return _includedFiles.Contains(fileName);
        };
    }
}
