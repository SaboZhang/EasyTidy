using CommunityToolkit.WinUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Util;

public static class I18n
{
    public static string Format(string resourceKey, params object[] args)
    {
        var template = resourceKey.GetLocalized();
        return string.Format(template, args);
    }
}
