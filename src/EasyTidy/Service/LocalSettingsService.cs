using System.Text.Json;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Util.UtilInterface;
using Microsoft.Extensions.Options;
using Windows.Storage;

namespace EasyTidy.Service;

public class LocalSettingsService : ILocalSettingsService
{
    private readonly string _defaultApplicationDataFolder = Constants.AppName;
    private const string _defaultLocalSettingsFile = "CommonApplicationData.json";

    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private readonly string _applicationDataFolder;
    private readonly string _localsettingsFile;

    private IDictionary<string, object> _settings;

    private CoreSettings _coreSettings;
    private HotkeysCollection _hotkeysCollection;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _applicationDataFolder = Path.Combine(_localApplicationData, _options.ApplicationDataFolder ?? _defaultApplicationDataFolder);
        _localsettingsFile = _options.LocalSettingsFile ?? _defaultLocalSettingsFile;

        _settings = new Dictionary<string, object>();
        _coreSettings = new CoreSettings();
        _hotkeysCollection = new HotkeysCollection();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localsettingsFile)) ?? new Dictionary<string, object>();

            _coreSettings = await Task.Run(() => _fileService.Read(_applicationDataFolder, _localsettingsFile)) ?? new CoreSettings();

            _hotkeysCollection = await Task.Run(() => _fileService.Read<HotkeysCollection>(_applicationDataFolder, "Hotkeys.json")) ?? new HotkeysCollection();

            _isInitialized = true;
        }
    }

    //public async Task<T> LoadSettingsAsync<T>() where T : class
    //{
    //    var key = typeof(T).Name; // 使用类型的完全限定名作为键

    //    if (string.IsNullOrEmpty(key))
    //        throw new ArgumentException("Invalid type for settings.");

    //    if (RuntimeHelper.IsMSIX)
    //    {
    //        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
    //        {
    //            return await Json.ToObjectAsync<T>((string)obj); // 反序列化为目标类型
    //        }
    //    }
    //    else
    //    {
    //        await InitializeAsync();

    //        if (_settings != null && _settings.TryGetValue(key, out var obj))
    //        {
    //            return await Json.ToObjectAsync<T>((string)obj); // 反序列化为目标类型
    //        }
    //    }

    //    return default; // 如果未找到键，返回默认值
    //}

    //public async Task SaveSettingsAsync<T>(T settings) where T : class
    //{
    //    var key = typeof(T).Name; // 使用类型的完全限定名作为键

    //    if (string.IsNullOrEmpty(key))
    //        throw new ArgumentException("Invalid type for settings.");

    //    var json = await Json.StringifyAsync(settings); // 序列化对象为 JSON

    //    if (RuntimeHelper.IsMSIX)
    //    {
    //        ApplicationData.Current.LocalSettings.Values[key] = json;
    //    }
    //    else
    //    {
    //        await InitializeAsync();

    //        _settings[key] = json;

    //        await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings));
    //    }
    //}

    public async Task SaveSettingsAsync(CoreSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // 使用类型名称作为键
        var key = nameof(CoreSettings);

        // 序列化对象为 JSON
        var json = await Json.StringifyAsync(settings);

        if (RuntimeHelper.IsMSIX)
        {
            // 保存到应用的本地设置存储中
            ApplicationData.Current.LocalSettings.Values[key] = json;
        }
        else
        {
            // 初始化设置文件存储
            await InitializeAsync();

            // 将字典保存到文件中
            await Task.Run(() =>
                _fileService.Save(_applicationDataFolder, _localsettingsFile, settings)
            );
        }
    }

    public async Task<CoreSettings> ReadSettingsAsync()
    {
        var key = nameof(CoreSettings); // 使用类型名称作为键

        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<CoreSettings>((string)obj); // 反序列化为 CoreSettings 类型
            }
        }
        else
        {
            await InitializeAsync();

            return _coreSettings;
        }

        return new CoreSettings(); // 如果未找到键，返回默认值
    }

    public async Task<T> LoadSettingsAsync<T>(string fileName = null) where T : class
    {
        var key = typeof(T).Name ?? throw new ArgumentException("Invalid type");

        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
                return await Json.ToObjectAsync<T>((string)obj);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                await InitializeAsync();
                if (_settings.TryGetValue(key, out var obj))
                    return await Json.ToObjectAsync<T>((string)obj);
            }
            else
            {
                var dict = await Task.Run(() =>
                    _fileService.Read<Dictionary<string, object>>(_applicationDataFolder, fileName)
                ) ?? new Dictionary<string, object>();

                if (dict.TryGetValue(key, out var obj))
                    return JsonSerializer.Deserialize<T>(obj.ToString()!);
            }
        }

        // 如果没找到，返回 new T()
        return default;
    }

    public async Task SaveSettingsAsync<T>(T settings, string fileName = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(settings);

        var key = typeof(T).Name ?? throw new ArgumentException("Invalid type");

        var json = await Json.StringifyAsync(settings);

        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = json;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                await InitializeAsync();
                _settings[key] = json;
                await Task.Run(() =>
                    _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings)
                );
            }
            else
            {
                var dict = new Dictionary<string, object> { [key] = settings };
                await Task.Run(() =>
                    _fileService.Save(_applicationDataFolder, fileName, dict)
                );
            }
        }
    }
}
