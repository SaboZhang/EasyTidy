using EasyTidy.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Threading.Tasks;

namespace EasyTidy.Util;

public class Json
{
    public static async Task<T> ToObjectAsync<T>(string value)
    {
        return await Task.Run<T>(() =>
        {
            return JsonConvert.DeserializeObject<T>(value);
        });
    }

    public static async Task<string> StringifyAsync(object value)
    {
        return await Task.Run<string>(() =>
        {
            return JsonConvert.SerializeObject(value);
        });
    }

    public static string SerializeForModel(object data, PropertyCase propertyCase)
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = propertyCase switch
            {
                PropertyCase.PascalCase => new DefaultContractResolver // PascalCase
                {
                    NamingStrategy = new DefaultNamingStrategy()
                },
                PropertyCase.CamelCase => new DefaultContractResolver // camelCase
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                _ => throw new NotSupportedException("Unsupported model type")
            },
            NullValueHandling = NullValueHandling.Ignore
        };

        return JsonConvert.SerializeObject(data, settings);
    }

}
