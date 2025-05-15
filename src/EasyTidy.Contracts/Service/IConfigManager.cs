using System;
using System.Threading.Tasks;

namespace EasyTidy.Contracts.Service;

public interface IConfigManager
{
    /// <summary>
    /// 加载指定类型的配置对象。
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="fileName">配置文件名</param>
    Task<T?> LoadAsync<T>(string fileName) where T : class;

    /// <summary>
    /// 保存指定类型的配置对象。
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="settings">配置对象</param>
    /// <param name="fileName">配置文件名</param>
    Task SaveAsync<T>(T settings, string fileName) where T : class;

    Task<T> LoadOrCreateAsync<T>(string fileName, Func<T>? defaultFactory = null) where T : class;

    /// <summary>
    /// 启动时初始化指定配置集合
    /// </summary>
    Task InitializeAsync(params (string fileName, Type configType, Func<object>? defaultFactory)[] configs);
}
