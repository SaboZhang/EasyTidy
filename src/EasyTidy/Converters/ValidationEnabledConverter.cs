using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Data;

namespace EasyTidy.Converters;

/// <summary>
/// ValidationEnabledConverter class that implements IValueConverter.
/// see <see cref="see <see cref="https://github.com/ghost1372/DevWinUI/tree/v8.2.0/dev/DevWinUI.Controls/Controls/Validation"/> "/>
/// </summary>
public sealed partial class ValidationEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is INotifyDataErrorInfo ? true : (object)false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
