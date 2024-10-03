using Microsoft.UI.Xaml.Data;

namespace EasyTidy.Converters;

public class BoolToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "是" : "否";
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string strValue)
        {
            if ("是".Equals(strValue))
            {
                return true;
            }
            else if ("否".Equals(strValue))
            {
                return false;
            }
        }
        return null;
    }
}
