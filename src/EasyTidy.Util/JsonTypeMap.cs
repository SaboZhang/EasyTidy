using System;
using System.Collections.Generic;

namespace EasyTidy.Util;

public static class JsonTypeMap
{
    private static readonly Dictionary<Type, Type> InterfaceToConcreteMap = new()
    {
        { typeof(IDictionary<string, object>), typeof(Dictionary<string, object>) },
        { typeof(IList<>), typeof(List<>) },
        { typeof(IEnumerable<>), typeof(List<>) },
        { typeof(ICollection<>), typeof(List<>) }
        // 以后有别的接口实现，也在这里加
    };

    public static Type GetConcreteType(Type interfaceType)
    {
        if (interfaceType.IsGenericType)
        {
            var genericDef = interfaceType.GetGenericTypeDefinition();

            if (InterfaceToConcreteMap.TryGetValue(genericDef, out var concreteDef))
            {
                var genericArgs = interfaceType.GetGenericArguments();
                return concreteDef.MakeGenericType(genericArgs);
            }
        }

        if (InterfaceToConcreteMap.TryGetValue(interfaceType, out var concreteType))
        {
            return concreteType;
        }

        return null;
    }
}
