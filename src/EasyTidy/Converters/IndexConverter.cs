using System;
using EasyTidy.Model;

namespace EasyTidy.Converters;

public class IndexConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TaskOrchestrationTable item && parameter is ListView listView)
        {
            var index = listView.Items.IndexOf(item);
            return index + 1; // 返回序号，+1 使序号从 1 开始
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
