using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyTidy.Model;
using Microsoft.UI.Xaml.Data;

namespace EasyTidy.Converters;

public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            BackupType backupType => backupType switch
            {
                BackupType.Local => Visibility.Collapsed,
                _ => Visibility.Visible
            },
            _ => Visibility.Collapsed
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
