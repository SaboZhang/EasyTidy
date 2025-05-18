using EasyTidy.Model;

namespace EasyTidy.Service;

public class LocalSettingsOptions
{
    public string? ApplicationDataFolder
    {
        get; set;
    }

    public string? LocalSettingsFile
    {
        get; set;
    }

    private static readonly Dictionary<Type, string> FileNames = new()
    {
        { typeof(HotkeysCollection), "Hotkeys.json" },
        { typeof(CoreSettings), "CommonApplicationData.json" },
    };

    public static string GetFileName<T>()
    {
        var type = typeof(T);
        return FileNames.TryGetValue(type, out var fileName)
            ? fileName
            : throw new InvalidOperationException($"No file mapping found for {type.Name}");
    }
}
