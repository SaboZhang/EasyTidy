using EasyTidy.Model;
using EasyTidy.Util.SettingsInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyTidy.Service;

public class FileService : IFileService
{
    public T Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            Config = (string.IsNullOrEmpty(json) ? new CoreSettings() : JsonSerializer.Deserialize<CoreSettings>(json, AppDataJsonContext.Default.CoreSettings)) ?? new CoreSettings();
            return string.IsNullOrEmpty(json) ? Activator.CreateInstance<T>() : JsonSerializer.Deserialize<T>(json);
        }

        return default;
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonSerializer.Serialize(content, AppDataJsonContext.Default.CoreSettings);
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
}
