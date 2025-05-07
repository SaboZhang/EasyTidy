using EasyTidy.Model;
using System;

namespace EasyTidy.Util.UtilInterface;

public interface IFileFilter
{
    Func<string, bool> GenerateFilter(FilterTable filter);
}
