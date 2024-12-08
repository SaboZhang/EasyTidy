using CommunityToolkit.WinUI.Converters;
using Microsoft.UI.Xaml.Data;

namespace EasyTidy.Converters;
public class Bool2Negation2VisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool newValue)
        {
            newValue = !newValue;
            BoolToVisibilityConverter boolToVisibilityConverter = new BoolToVisibilityConverter();
            return boolToVisibilityConverter.Convert(newValue, targetType, parameter, language);
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
