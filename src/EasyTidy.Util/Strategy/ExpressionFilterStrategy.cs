using EasyTidy.Util.UtilInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace EasyTidy.Util.Strategy;

public class ExpressionFilterStrategy : IFilterStrategy
{
    public IEnumerable<Func<string, bool>> GenerateFilters(string rule)
    {
        yield return filePath => Regex.IsMatch(Path.GetFileName(filePath), rule);
    }
}
