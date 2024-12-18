using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            string content = File.ReadAllText(filePath);
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
}
