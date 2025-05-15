using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using Windows.Storage;

namespace EasyTidy.Service;

public class ConfigManager : IConfigManager
{
    private readonly IFileService _fileService;
    private readonly string _baseFolder;
    private readonly bool _isMSIX;

    public ConfigManager(IFileService fileService)
    {
        _fileService = fileService;
        _baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.AppName);
        _isMSIX = RuntimeHelper.IsMSIX;
    }

    public async Task<T?> LoadAsync<T>(string fileName) where T : class
    {
        var key = typeof(T).FullName ?? throw new InvalidOperationException("Invalid type.");

        if (_isMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj);
            }
        }
        else
        {
            var dict = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_baseFolder, fileName))
                        ?? new Dictionary<string, object>();

            if (dict.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj);
            }
        }

        return default;
    }

    public async Task SaveAsync<T>(T settings, string fileName) where T : class
    {
        var key = typeof(T).FullName ?? throw new InvalidOperationException("Invalid type.");
        var json = await Json.StringifyAsync(settings);

        if (_isMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = json;
        }
        else
        {
            var dict = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_baseFolder, fileName))
                        ?? new Dictionary<string, object>();

            dict[key] = json;

            await Task.Run(() => _fileService.Save(_baseFolder, fileName, dict));
        }
    }

    public async Task<T> LoadOrCreateAsync<T>(string fileName, Func<T>? defaultFactory = null) where T : class
    {
        var key = typeof(T).FullName ?? throw new InvalidOperationException("Invalid type.");

        if (_isMSIX)
        {
            return await LoadFromLocalSettingsAsync<T>(key, defaultFactory);
        }

        return await LoadFromFileSettingsAsync<T>(fileName, key, defaultFactory);
    }

    private async Task<T> LoadFromLocalSettingsAsync<T>(string key, Func<T>? defaultFactory) where T : class
    {
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
        {
            return await DeserializeOrDefault<T>((string)obj, defaultFactory);
        }

        var defaultInstance = CreateDefault(defaultFactory);
        var json = await Json.StringifyAsync(defaultInstance);
        ApplicationData.Current.LocalSettings.Values[key] = json;
        return defaultInstance;
    }

    private async Task<T> LoadFromFileSettingsAsync<T>(string fileName, string key, Func<T>? defaultFactory) where T : class
    {
        var dict = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_baseFolder, fileName))
                   ?? new Dictionary<string, object>();

        if (dict.TryGetValue(key, out var obj))
        {
            return await DeserializeOrDefault<T>((string)obj, defaultFactory);
        }

        var defaultInstance = CreateDefault(defaultFactory);
        var json = await Json.StringifyAsync(defaultInstance);
        dict[key] = json;
        await Task.Run(() => _fileService.Save(_baseFolder, fileName, dict));
        return defaultInstance;
    }

    private async Task<T> DeserializeOrDefault<T>(string json, Func<T>? defaultFactory) where T : class
    {
        return await Json.ToObjectAsync<T>(json)
            ?? defaultFactory?.Invoke()
            ?? throw new Exception($"反序列化 {typeof(T).FullName} 失败，且无法创建默认实例");
    }

    private T CreateDefault<T>(Func<T>? factory) where T : class
    {
        return factory?.Invoke() ?? Activator.CreateInstance<T>()
            ?? throw new Exception($"无法创建类型 {typeof(T).FullName} 的默认实例");
    }

    public async Task InitializeAsync(params (string fileName, Type configType, Func<object>? defaultFactory)[] configs)
    {
        foreach (var (fileName, type, factory) in configs)
        {
            var method = typeof(ConfigManager).GetMethod(nameof(LoadOrCreateAsync))?
                .MakeGenericMethod(type);
            if (method != null)
            {
                await (Task)method.Invoke(this, new object?[] { fileName, factory })!;
            }
        }
    }

}
