using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Util.Strategy;

public class DateFilter : IFileFilter
{
    private readonly string _dateValue;
    private readonly DateUnit _dateUnit;
    private readonly ComparisonResult _dateOperator;
    private readonly DateType _dateType;

    public DateFilter(string dateValue, DateUnit dateUnit, ComparisonResult dateOperator, DateType dateType)
    {
        _dateValue = dateValue;
        _dateUnit = dateUnit;
        _dateOperator = dateOperator;
        _dateType = dateType;
    }

    public Func<string, bool> GenerateFilter(FilterTable filter)
    {
        return filePath =>
        {
            DateTime fileDate = _dateType switch
            {
                DateType.Create => File.GetCreationTime(filePath),
                DateType.Edit => File.GetLastWriteTime(filePath),
                DateType.Visit => File.GetLastAccessTime(filePath),
                _ => DateTime.Now
            };

            var result = FilterUtil.ConvertToDateTime(_dateValue, _dateUnit);
            if (result is Tuple<DateTime, DateTime> dateTuple)
            {
                return FilterUtil.CompareDates(fileDate, _dateOperator, dateTuple.Item1, dateTuple.Item2);
            }
            else if (result is DateTime singleDate)
            {
                return FilterUtil.CompareDates(fileDate,_dateOperator, singleDate);
            }
            return FilterUtil.CompareDates(fileDate, _dateOperator, DateTime.Now);
        };
    }
}
