using Microsoft.UI.Xaml.Data;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EasyTidy.Converters;

public class EnumDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Enum enumValue)
        {
            MemberInfo member = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();
            if (member != null)
            {
                var displayAttribute = member.GetCustomAttribute<DisplayAttribute>();
                if (displayAttribute != null)
                {
                    return displayAttribute.Name;
                }
            }
        }

        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
