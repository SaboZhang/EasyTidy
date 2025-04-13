using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Converters;

public class EnumToImageConverter : IValueConverter
{
    public string? Prefix { get; set; } = "ms-appx:///Assets/AI/";
    public string? Suffix { get; set; } = ".png";
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || value.GetType().IsEnum == false)
            return null;

        string fileName = value.ToString().ToLower();
        string uri = $"{Prefix}{fileName}{Suffix}";
        return new BitmapImage(new Uri(uri));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
