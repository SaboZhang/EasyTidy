using System;
using System.Collections.Generic;
using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;

namespace EasyTidy.Util.Strategy;

public class FolderFilterStrategy : IFilterStrategy
{
    public IEnumerable<Func<string, bool>> GenerateFilters(string rule)
    {
        throw new NotImplementedException();
    }
}
