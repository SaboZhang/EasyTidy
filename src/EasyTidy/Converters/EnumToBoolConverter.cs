using EasyTidy.Model;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Converters;

public class BoolToEnumConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null)
            return false;

        var enumString = parameter.ToString();
        if (value.GetType().IsEnum && Enum.TryParse(value.GetType(), enumString, out var enumValue))
        {
            return value.Equals(enumValue);
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue && boolValue && parameter != null && targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, parameter.ToString(), out var enumValue))
            {
                return enumValue;
            }
        }

        // 返回 null，表示不变或无效选择（WinUI 中推荐手动处理 null）
        return PromptType.BuiltIn;
    }
}
