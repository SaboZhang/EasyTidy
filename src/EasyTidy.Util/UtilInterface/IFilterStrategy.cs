using System;
using System.Collections.Generic;

namespace EasyTidy.Util.UtilInterface;

public interface IFilterStrategy
{
     IEnumerable<Func<string, bool>> GenerateFilters(string rule);
}
