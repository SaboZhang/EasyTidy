using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System;
using System.IO;

namespace EasyTidy.Util.Strategy;

public class AttributeFilter : IFileFilter
{
    private readonly FileAttributes _attribute;
    private readonly YesOrNo _value;

    public AttributeFilter(FileAttributes attribute, YesOrNo value)
    {
        _attribute = attribute;
        _value = value;
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

            // 判断是文件还是文件夹，并获取其属性
            FileAttributes attributes = GetAttributes(path);

            // 检查属性是否匹配
            bool hasFlag = attributes.HasFlag(_attribute);
            return hasFlag == (_value == YesOrNo.Yes);
        };
    }

    // 获取文件或文件夹的属性
    private FileAttributes GetAttributes(string path)
    {
        if (File.Exists(path))
        {
            return new FileInfo(path).Attributes;
        }
        else if (Directory.Exists(path))
        {
            return new DirectoryInfo(path).Attributes;
        }
        else
        {
            throw new FileNotFoundException($"The path '{path}' does not exist as a file or directory.");
        }
    }

}
