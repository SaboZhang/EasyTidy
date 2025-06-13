using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EasyTidy.Util.Strategy;

public class ContentFilter : IFileFilter
{
    private readonly string _contentValue;
    private readonly ContentOperatorEnum _operator;

    public ContentFilter(string contentValue, ContentOperatorEnum opera)
    {
        _contentValue = contentValue;
        _operator = opera;
    }

    public Func<string, bool> GenerateFilter(FilterTable filter)
    {
        return filePath =>
        {
            string? content = TryExtractContent(filePath);
            if (string.IsNullOrWhiteSpace(content))
                return false;
            return _operator switch
            {
                ContentOperatorEnum.AtLeastOneWord => content.Split(' ').Any(word => word.Equals(_contentValue, StringComparison.OrdinalIgnoreCase)),
                ContentOperatorEnum.AllWordsInAnyOrder => _contentValue.Split(' ').All(word => content.Contains(word, StringComparison.OrdinalIgnoreCase)),
                ContentOperatorEnum.RegularExpression => Regex.IsMatch(content, _contentValue),
                ContentOperatorEnum.String => content.Contains(_contentValue, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        };
    }

    private string? TryExtractContent(string filePath)
    {
        try
        {
            string extension = Path.GetExtension(filePath).ToLower();

            return extension switch
            {
                ".txt" => File.ReadAllText(filePath),

                ".pdf" => FileReader.ExtractTextFromPdf(filePath),

                ".docx" => FileReader.ReadWord(filePath),
                ".doc" => FileReader.ReadWord(filePath),

                ".xlsx" => FlattenExcelContent(FileReader.ReadExcel(filePath)),
                ".xls" => FlattenExcelContent(FileReader.ReadExcel(filePath)),

                _ => null
            };
        }
        catch (Exception ex)
        {
            LogService.Logger.Warn($"无法读取文件内容: {filePath}, 错误信息: {ex.Message}");
            return null;
        }
    }

    private string FlattenExcelContent(List<List<string>> rows)
    {
        return string.Join("\n", rows.Select(line => string.Join("\t", line)));
    }
}
