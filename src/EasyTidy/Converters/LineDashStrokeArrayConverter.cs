using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace EasyTidy.Converters;

public class LineDashStrokeArrayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DoubleCollection collection)
        {
            var doubleCollection = new DoubleCollection();
            foreach (var item in collection)
            {
                doubleCollection.Add(item);
            }
            return doubleCollection;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
