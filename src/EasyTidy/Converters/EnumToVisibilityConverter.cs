using EasyTidy.Model;
using Microsoft.UI.Xaml.Data;

namespace EasyTidy.Converters;

public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        string enumValue = value.ToString();
        string targetValue = parameter.ToString();

        if (string.Equals(enumValue, targetValue, StringComparison.OrdinalIgnoreCase))
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility &&
            visibility == Visibility.Visible &&
            parameter is string enumName &&
            targetType.IsEnum &&
            Enum.TryParse(targetType, enumName, out var enumValue))
        {
            return enumValue;
        }

        return DependencyProperty.UnsetValue;
    }
}
