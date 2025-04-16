using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Data;

namespace EasyTidy.Converters;

public partial class BoolToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "YesStr".GetLocalized() : "NoStr".GetLocalized();
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string strValue)
        {
            if ("YesStr".GetLocalized().Equals(strValue))
            {
                return true;
            }
            else if ("NoStr".GetLocalized().Equals(strValue))
            {
                return false;
            }
        }
        return null;
    }
}
