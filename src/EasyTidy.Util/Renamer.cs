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

    /// <summary>
    /// 解析模板参数
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string ParseTemplate(string source, string target)
    {
        DateTime? creationTime = File.Exists(source) ? File.GetCreationTime(source) : Directory.GetCreationTime(source);

        if (!TemplateRegex().IsMatch(target))
        {
            return target; // 无模板参数，直接返回原字符串
        }

        // 使用正则表达式解析 ${} 模板
        string result = TemplateRegex().Replace(target, match =>
        {
            string[] parameters = match.Groups[1].Value.Split(',');

            // 判断不同的模板参数类型并处理
            if (match.Value.Contains(INCREMENT_PARAMETER) || match.Value.Contains("start") || match.Value.Contains("padding") || match.Value.Equals("${}"))
            {
                return ParseIncrement(parameters);
            }
            else if (match.Value.Contains(RSTRING_ALNUM_PARAMETER))
            {
                return ParseRandom(parameters, "alnum");
            }
            else if (match.Value.Contains(RSTRING_ALPHA_PARAMETER))
            {
                return ParseRandom(parameters, "alpha");
            }
            else if (match.Value.Contains(RSTRING_DIGIT_PARAMETER))
            {
                return ParseRandom(parameters, "digit");
            }
            else if (match.Value.Contains(RUUIDV4_PARAMETER))
            {
                return GenerateUUID();
            }
            return match.Value;
        });

        // 替换日期模板
        result = ReplaceDateTemplates(result, creationTime);

        return result;
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
}
