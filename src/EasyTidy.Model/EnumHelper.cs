using CommunityToolkit.WinUI;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

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
                return displayAttribute.Name?.GetLocalized();
            }
        }
        return value.ToString();
    }

    public static T? ParseEnum<T>(string value, bool ignoreCase = true) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, ignoreCase, out var result))
        {
            return result;
        }
        return null;
    }

    public static bool TryParseDisplayName<TEnum>(string input, out TEnum result) where TEnum : struct, Enum
    {
        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            // 比较 DisplayAttribute.Name
            var displayAttr = field.GetCustomAttribute<DisplayAttribute>();
            if (displayAttr != null && displayAttr.Name?.GetLocalized().Equals(input, StringComparison.OrdinalIgnoreCase) == true)
            {
                result = (TEnum)field.GetValue(null)!;
                return true;
            }

            // 比较枚举名本身
            if (field.Name.Equals(input, StringComparison.OrdinalIgnoreCase))
            {
                result = (TEnum)field.GetValue(null)!;
                return true;
            }
        }

        result = default;
        return false;
    }

}
