using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Model;

public static class EnumHelper
{
    /// <summary>
    /// 获取枚举成员的显示名称
    /// </summary>
    /// <param name="value">枚举值</param>
    /// <returns>显示名称</returns>
    public static string GetDisplayName(Enum value)
    {
        var type = value.GetType();
        var memberInfo = type.GetMember(value.ToString());
        if (memberInfo.Length > 0)
        {
            var attributes = memberInfo[0].GetCustomAttributes(typeof(DisplayAttribute), false);
            if (attributes.Length > 0)
            {
                var displayAttribute = (DisplayAttribute)attributes[0];
                return displayAttribute.Name;
            }
        }
        return value.ToString();
    }
}
