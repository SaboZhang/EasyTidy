using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Util;
using EasyTidy.Util.SettingsInterface;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace EasyTidy.Service;

public class LocalSettingsService : ILocalSettingsService
{
    private const string _defaultApplicationDataFolder = "EasyTidy";
    private const string _defaultLocalSettingsFile = "CommonApplicationData.json";

    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private readonly string _applicationDataFolder;
    private readonly string _localsettingsFile;

    private IDictionary<string, object> _settings;

    private CoreSettings _coreSettings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _applicationDataFolder = Path.Combine(_localApplicationData, _options.ApplicationDataFolder ?? _defaultApplicationDataFolder);
        _localsettingsFile = _options.LocalSettingsFile ?? _defaultLocalSettingsFile;

        _settings = new Dictionary<string, object>();
        _coreSettings = new CoreSettings();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localsettingsFile)) ?? new Dictionary<string, object>();

            _coreSettings = await Task.Run(() => _fileService.Read(_applicationDataFolder, _localsettingsFile)) ?? new CoreSettings();

            _isInitialized = true;
        }
    }

    public async Task<T> LoadSettingsAsync<T>() where T : class
    {
        var key = typeof(T).FullName; // 使用类型的完全限定名作为键

        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Invalid type for settings.");

        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj); // 反序列化为目标类型
            }
        }
        else
        {
            await InitializeAsync();

            if (_settings != null && _settings.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj); // 反序列化为目标类型
            }
        }

        return default; // 如果未找到键，返回默认值
    }

    public async Task SaveSettingsAsync<T>(T settings) where T : class
    {
        var key = typeof(T).FullName; // 使用类型的完全限定名作为键

        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Invalid type for settings.");

        var json = await Json.StringifyAsync(settings); // 序列化对象为 JSON

        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = json;
        }
        else
        {
            await InitializeAsync();

            _settings[key] = json;

            await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings));
        }
    }

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
}
