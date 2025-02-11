using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EasyTidy.Util;

public partial class Renamer
{
    private static int _currentStart = 0;

    // 定义模板参数类型相关的常量
    private const string INCREMENT_PARAMETER = "increment";
    private const string RSTRING_ALNUM_PARAMETER = "rstringalnum";
    private const string RSTRING_ALPHA_PARAMETER = "rstringalpha";
    private const string RSTRING_DIGIT_PARAMETER = "rstringdigit";
    private const string RUUIDV4_PARAMETER = "ruuidv4";
    private const string FOLDER_PARAMETER = "source";
    private const string PARENT_PARAMETER = "parent";
    private const string REGEX_PARAMETER = "regex";
    private const string REPLACE_PARAMETER = "replace";

    /// <summary>
    /// 解析模板参数
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string ParseTemplate(string source, string target)
    {
        DateTime? creationTime = File.Exists(source) ? File.GetCreationTime(source) : Directory.GetCreationTime(source);

        if (!target.Contains('$'))
        {
            return target; // 无模板参数，直接返回原字符串
        }

        // 使用正则表达式解析 ${} 模板
        string result = TemplateRegex().Replace(target, match =>
        {
            string[] parameters = match.Groups[1].Value.Split(',');

            // 获取处理方法
            Func<string[], string> templateHandler = GetTemplateHandler(match.Value, source);

            // 执行模板处理
            return templateHandler != null ? templateHandler(parameters) : match.Value;
        });

        // 替换日期模板
        result = ReplaceDateTemplates(result, creationTime);

        return File.Exists(source) ? ReplaceFileName(source, result) : result;
    }

    // 获取模板参数处理方法
    private static Func<string[], string> GetTemplateHandler(string matchValue, string source)
    {
        if (matchValue.Contains(INCREMENT_PARAMETER) || matchValue.Contains("start") || matchValue.Contains("padding") || matchValue.Equals("${}"))
        {
            return ParseIncrement;
        }
        else if (matchValue.Contains(RSTRING_ALNUM_PARAMETER))
        {
            return parameters => ParseRandom(parameters, "alnum");
        }
        else if (matchValue.Contains(RSTRING_ALPHA_PARAMETER))
        {
            return parameters => ParseRandom(parameters, "alpha");
        }
        else if (matchValue.Contains(RSTRING_DIGIT_PARAMETER))
        {
            return parameters => ParseRandom(parameters, "digit");
        }
        else if (matchValue.Contains(RUUIDV4_PARAMETER))
        {
            return _ => GenerateUUID();
        }
        else if (matchValue.Contains(PARENT_PARAMETER) || matchValue.Contains(FOLDER_PARAMETER))
        {
            return _ => GetFolderNameFromSource(source, matchValue);
        }
        else if (matchValue.Contains(REGEX_PARAMETER))
        {
            return parameters => RegexReplaceHandler(parameters, source);
        }
        else if (matchValue.Contains(REPLACE_PARAMETER))
        {
            return parameters => ReplaceHandler(parameters, source);
        }

        return null; // 如果没有匹配到处理方法，返回null
    }

    /// <summary>
    /// 根据传入的 source（文件或目录路径）获取目录名称。
    /// 如果 source 是目录，则返回其名称；
    /// 如果 source 是文件，则返回文件所在目录的名称；
    /// 如果 source 不存在，则返回空字符串。
    /// </summary>
    /// <param name="source">文件或目录的完整路径</param>
    /// <returns>目录名称或空字符串</returns>
    private static string GetFolderNameFromSource(string source, string matchValue)
    {
        if (Directory.Exists(source))
        {
            // 获取上一级目录名称
            if (matchValue.Contains(PARENT_PARAMETER))
            {
                return GetParentFolderName(source);
            }
            // 其他情况直接返回文件夹名称
            return new DirectoryInfo(source).Name;
        }
        else if (File.Exists(source) && matchValue.Contains(PARENT_PARAMETER))
        {
            // source 为文件，获取其所在目录
            string directory = Path.GetDirectoryName(source);
            return !string.IsNullOrEmpty(directory) ? new DirectoryInfo(directory).Name : string.Empty;
        }
        else if (File.Exists(source) && matchValue.Contains(FOLDER_PARAMETER))
        {
            // source 为文件，获取其所在目录
            string fileName = Path.GetFileNameWithoutExtension(source);
            return !string.IsNullOrEmpty(fileName) ? fileName : string.Empty;
        }
        return string.Empty;
    }

    private static string GetParentFolderName(string source)
    {
        if (Directory.Exists(source))
        {
            DirectoryInfo parentDirectory = Directory.GetParent(source);
            return parentDirectory?.Name ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// 使用生成的新文件名替换 source 文件路径中的文件名，并返回完整路径
    /// </summary>
    /// <param name="source"></param>
    /// <param name="newFileName"></param>
    /// <returns></returns>
    private static string ReplaceFileName(string source, string newFileName)
    {
        string sourceDirectory = Path.GetDirectoryName(source) ?? string.Empty;
        string sourceExtension = Path.GetExtension(source);
        return Path.Combine(sourceDirectory, newFileName + sourceExtension);
    }

    /// <summary>
    /// 替换日期模板
    /// </summary>
    /// <param name="result"></param>
    /// <param name="creationTime"></param>
    /// <returns></returns>
    private static string ReplaceDateTemplates(string result, DateTime? creationTime)
    {
        var dateTemplates = new Dictionary<string, Func<DateTime?, string>>
        {
            { "$YYYY", dt => dt?.ToString("yyyy") ?? "0000" },
            { "$YY", dt => dt?.ToString("yy") ?? "00" },
            { "$Y", dt => dt?.ToString("yy")?.LastOrDefault().ToString() ?? "0" },
            { "$MMMM", dt => dt?.ToString("MMMM") ?? "MMMM" },
            { "$MMM", dt => dt?.ToString("MMM") ?? "MMM" },
            { "$MM", dt => dt?.ToString("MM") ?? "00" },
            { "$M", dt => dt?.ToString("M") ?? "0" },
            { "$DDDD", dt => dt?.ToString("dddd") ?? "DDDD" },
            { "$DDD", dt => dt?.ToString("ddd") ?? "DDD" },
            { "$DD", dt => dt?.ToString("dd") ?? "00" },
            { "$D", dt => dt?.ToString("d") ?? "0" },
            { "$hh", dt => dt?.ToString("HH") ?? "00" },
            { "$h", dt => dt?.ToString("H") ?? "0" },
            { "$mm", dt => dt?.ToString("mm") ?? "00" },
            { "$m", dt => dt?.ToString("m") ?? "0" },
            { "$ss", dt => dt?.ToString("ss") ?? "00" },
            { "$s", dt => dt?.ToString("s") ?? "0" },
            { "$fff", dt => dt?.ToString("fff") ?? "000" },
            { "$ff", dt => dt?.ToString("ff") ?? "00" },
            { "$f", dt => dt?.ToString("f") ?? "0" }
        };

        foreach (var kvp in dateTemplates)
        {
            if (result.Contains(kvp.Key))
            {
                result = result.Replace(kvp.Key, kvp.Value(creationTime));
            }
        }

        return result;
    }

    /// <summary>
    /// 解析计数器
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static string ParseIncrement(string[] parameters)
    {
        int start = 0, step = 1, padding = 0;

        foreach (string param in parameters)
        {
            if (param.StartsWith("start=", StringComparison.OrdinalIgnoreCase))
            {
                start = int.Parse(param.Substring("start=".Length));
            }
            else if (param.StartsWith("increment=", StringComparison.OrdinalIgnoreCase))
            {
                step = int.Parse(param.Substring("increment=".Length));
            }
            else if (param.StartsWith("padding=", StringComparison.OrdinalIgnoreCase))
            {
                padding = int.Parse(param.Substring("padding=".Length));
            }
        }

        // 如果这是第一次调用，初始化 currentStart
        if (_currentStart == 0)
        {
            _currentStart = start;
        }

        // 计算当前值并更新 currentStart
        int currentValue = _currentStart;
        _currentStart += step;

        return currentValue.ToString().PadLeft(padding, '0');
    }

    // 重置 currentStart 的方法
    public static void ResetIncrement()
    {
        _currentStart = 0;
    }

    /// <summary>
    /// 生成随机值
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string ParseRandom(string[] parameters, string type)
    {
        int length = 8;

        foreach (string param in parameters)
        {
            if (param.Contains('='))
                length = int.Parse(param.Split('=')[1]);
        }

        return type switch
        {
            "alnum" => GenerateRandomString(length, true, true),
            "alpha" => GenerateRandomString(length, false, true),
            "digit" => GenerateRandomString(length, true, false),
            _ => throw new ArgumentException("Invalid random type")
        };
    }

    /// <summary>
    /// 生成随机字符
    /// </summary>
    /// <param name="length"></param>
    /// <param name="includeNumbers"></param>
    /// <param name="includeUpperCase"></param>
    /// <returns></returns>
    public static string GenerateRandomString(int length, bool includeNumbers = true, bool includeUpperCase = true)
    {
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numbers = "0123456789";

        StringBuilder characterSet = new StringBuilder(lowerChars);
        if (includeUpperCase) characterSet.Append(upperChars);
        if (includeNumbers) characterSet.Append(numbers);

        Random random = new Random();
        StringBuilder result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            int index = random.Next(characterSet.Length);
            result.Append(characterSet[index]);
        }

        return result.ToString();
    }

    /// <summary>
    /// 生成UUID
    /// </summary>
    /// <returns></returns>
    public static string GenerateUUID()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 正则匹配
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"\$\{(.*?)\}")]
    private static partial Regex TemplateRegex();

    /// <summary>
    /// 正则替换
    /// </summary>
    /// <param name="parameters">解析的参数</param>
    /// <param name="source">要修改的源文件或者文件夹名称</param>
    /// <returns></returns>
    private static string RegexReplaceHandler(string[] parameters, string source)
    {
        if (parameters.Length < 2) return string.Empty;

        // 路径预处理
        source = File.Exists(source) ? Path.GetFileNameWithoutExtension(source)
               : Directory.Exists(source) ? new DirectoryInfo(source).Name
               : source;

        string pattern = parameters[0].Replace("regex=", "");
        string replacement = parameters[1] ?? string.Empty;

        return Regex.Replace(source, pattern, replacement, RegexOptions.Compiled);
    }

    /// <summary>
    /// 替换处理
    /// </summary>
    /// <param name="parameters">替换参数</param>
    /// <param name="source">原名称</param>
    /// <returns></returns>
    private static string ReplaceHandler(string[] parameters, string source)
    {
        if (parameters.Length < 2) return string.Empty;

        // 路径预处理
        source = File.Exists(source) ? Path.GetFileNameWithoutExtension(source)
               : Directory.Exists(source) ? new DirectoryInfo(source).Name
               : source;

        string oldValue = parameters[0].Replace("replace=","");
        string newValue = parameters[1];
        bool caseSensitive = parameters.Length > 2 && bool.TryParse(parameters[2], out bool result) && result;

        StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        return ReplaceWithComparison(source, oldValue, newValue, comparison);
    }

    // 自定义字符串替换，支持大小写敏感控制
    private static string ReplaceWithComparison(string input, string oldValue, string newValue, StringComparison comparison)
    {
        int index = input.IndexOf(oldValue, comparison);
        if (index < 0)
        {
            return input; // 没找到匹配项，返回原始字符串
        }

        return string.Concat(input.AsSpan(0, index), newValue, input.AsSpan(index + oldValue.Length));
    }

}
