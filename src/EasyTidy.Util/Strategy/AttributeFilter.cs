using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Util.Strategy;

public class AttributeFilter : IFileFilter
{
    private readonly FileAttributes _attribute;
    private readonly YesOrNo _value;

    public AttributeFilter(FileAttributes attribute, YesOrNo value)
    {
        _attribute = attribute;
        _value = value;
    }

    public Func<string, bool> GenerateFilter(FilterTable filter)
    {
        return filePath => new FileInfo(filePath).Attributes.HasFlag(_attribute) == (_value == YesOrNo.Yes);
    }
}
