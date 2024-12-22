using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Util.UtilInterface;

public interface IFileFilter
{
    Func<string, bool> GenerateFilter(FilterTable filter);
}
