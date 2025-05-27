using System;
using System.Collections.Generic;
using EasyTidy.Model;

namespace EasyTidy.Util.UtilInterface;

public interface IFilterStrategy
{
    IEnumerable<Func<string, bool>> GenerateFilters(string rule);

    IEnumerable<FilterItem> GenerateFilterItems(string rule);
}
