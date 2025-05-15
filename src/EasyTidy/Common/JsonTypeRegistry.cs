using EasyTidy.Model;
using Org.BouncyCastle.Asn1.Tsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace EasyTidy.Common;

public static class JsonTypeRegistry
{
    private static readonly Dictionary<Type, JsonTypeInfo> _typeInfoMap = new()
    {
        [typeof(CoreSettings)] = AppDataJsonContext.Default.CoreSettings,
        [typeof(Hotkey)] = AppDataJsonContext.Default.Hotkey,
        [typeof(HotkeysCollection)] = AppDataJsonContext.Default.HotkeysCollection,
    };

    public static JsonTypeInfo GetTypeInfo(Type t)
    {
        if (_typeInfoMap.TryGetValue(t, out var info))
            return info;
        throw new NotSupportedException($"Type {t.FullName} is not registered in JsonTypeRegistry.");
    }
}
