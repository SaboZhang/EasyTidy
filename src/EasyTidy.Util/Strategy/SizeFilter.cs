using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Util.Strategy;

public class SizeFilter : IFileFilter
{
    private readonly string _sizeValue;
    private readonly SizeUnit _sizeUnit;
    private readonly ComparisonResult _sizeOperator;

    public SizeFilter(string sizeValue, SizeUnit sizeUnit, ComparisonResult sizeOperator)
    {
        _sizeValue = sizeValue;
        _sizeUnit = sizeUnit;
        _sizeOperator = sizeOperator;
    }

    public Func<string, bool> GenerateFilter(FilterTable filter)
    {
        return filePath =>
        {
            long fileSize = new FileInfo(filePath).Length;
            var (firstSize, secondSize) = FilterUtil.ConvertSizeToBytes(_sizeValue, _sizeUnit);
            return FilterUtil.CompareValues(fileSize, firstSize, secondSize, _sizeOperator);
        };
    }
}
