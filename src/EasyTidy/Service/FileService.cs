using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace EasyTidy.Service;

public class FileService : IFileService
{
    public T Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);

        if (!File.Exists(path))
            return default;

        var json = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return DeserializeWithFallback<T>(json, AppDataJsonContext.Default) ?? default;
        }
        catch
        {
            // 解析失败，返回默认值
            return default;
        }
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var type = typeof(T);

        var fileContent = JsonSerializer.Serialize(content, AppDataJsonContext.Default.GetTypeInfo(typeof(T))).Replace("\\u002B", "+");
        File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }

    public void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }

    public CoreSettings Read(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            Config = (string.IsNullOrEmpty(json) ? new CoreSettings() : JsonSerializer.Deserialize<CoreSettings>(json, AppDataJsonContext.Default.CoreSettings)) ?? new CoreSettings();
            return Config;
        }

        return default;
    }

    public static CoreSettings Config { get; set; }

    public static T DeserializeWithFallback<T>(string json, JsonSerializerContext context)
    {
        // 优先用 SourceGenerator TypeInfo
        var typeInfoObj = context.GetTypeInfo(typeof(T));
        if (typeInfoObj is JsonTypeInfo<T> typeInfo)
        {
            var result = JsonSerializer.Deserialize(json, typeInfo);
            if (result != null)
                return result;
        }

        // fallback：接口用映射的具体类型反序列化
        var concreteType = JsonTypeMap.GetConcreteType(typeof(T));
        if (concreteType != null)
        {
            var result = JsonSerializer.Deserialize(json, concreteType);
            if (result != null)
                return (T)result;
        }

        // 最后兜底
        return JsonSerializer.Deserialize<T>(json);
    }
}
