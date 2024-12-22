using CommunityToolkit.WinUI;
using EasyTidy.Log;
using EasyTidy.Model;
using EasyTidy.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTidy.Service;

public static class FileEventHandler
{
    private static ConcurrentDictionary<string, FileSystemWatcher> _watchers = [];
    private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // 控制并发的信号量
    // 缓存源路径和目标路径的字典
    private static ConcurrentDictionary<string, List<OperationParameters>> _sourceToTargetsCache = new ConcurrentDictionary<string, List<OperationParameters>>();
    private static ConcurrentDictionary<string, byte> _processingFiles = new ConcurrentDictionary<string, byte>();

    /// <summary>
    /// 监控文件变化
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="targetPath"></param>
    /// <param name="delaySeconds"></param>
    /// <param name="fileOperationType"></param>
    public static void MonitorFolder(OperationParameters parameter, int delaySeconds = 5)
    {
        UpdateSourceCache(parameter);

        if (_watchers.ContainsKey(parameter.SourcePath)) return; // 防止重复监控

        LogService.Logger.Info($"监控文件变化：{parameter.SourcePath}");

        // 创建并配置文件监控器
        var watcher = CreateFileSystemWatcher(parameter.SourcePath);

        watcher.IncludeSubdirectories = (bool)CommonUtil.Configs.GeneralConfig.SubFolder;
        // 绑定文件变化事件
        BindFileSystemEvents(watcher, delaySeconds, parameter);

        // 存储监控器
        _watchers[parameter.SourcePath] = watcher;
    }

    /// <summary>
    /// 更新源路径和目标路径的缓存
    /// </summary>
    /// <param name="parameter"></param>
    private static void UpdateSourceCache(OperationParameters parameter)
    {
        var operationParams = OperationParameters.CreateOperationParameters(parameter);

        _sourceToTargetsCache[parameter.SourcePath] = _sourceToTargetsCache.TryGetValue(parameter.SourcePath, out var targets)
            ? targets.Concat(new List<OperationParameters> { operationParams }).OrderByDescending(op => op.Priority).ThenBy(op => op.CreateTime).ToList()
            : new List<OperationParameters> { operationParams }.OrderByDescending(op => op.Priority).ThenBy(op => op.CreateTime).ToList();
    }

    /// <summary>
    /// 创建文件系统监控器
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <returns></returns>
    private static FileSystemWatcher CreateFileSystemWatcher(string sourcePath)
    {
        return new FileSystemWatcher
        {
            Path = sourcePath.Equals("DesktopText".GetLocalized())
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : sourcePath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
            Filter = "*.*"
        };
    }

    /// <summary>
    /// 绑定文件变化事件
    /// </summary>
    /// <param name="watcher"></param>
    /// <param name="delaySeconds"></param>
    /// <param name="parameter"></param>
    private static void BindFileSystemEvents(FileSystemWatcher watcher, int delaySeconds, OperationParameters parameter)
    {
        void onChange(FileSystemEventArgs e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, parameter));

        watcher.Created += (sender, e) => onChange(e);
        watcher.Deleted += (sender, e) => onChange(e);
        watcher.Changed += (sender, e) => onChange(e);
        watcher.Renamed += (sender, e) => onChange(e);
        watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// 延时执行
    /// </summary>
    /// <param name="e"></param>
    /// <param name="delaySeconds"></param>
    /// <param name="action"></param>
    private static void OnFileChange(FileSystemEventArgs e, int delaySeconds, Action action)
    {
        ThreadPool.QueueUserWorkItem(state =>
        {
            _semaphore.Wait();  // 确保一次只执行一个文件处理操作
            try
            {
                Thread.Sleep(delaySeconds * 1000);
                action();
            }
            finally
            {
                _semaphore.Release(); // 释放信号量
            }
        });
    }

    /// <summary>
    /// 处理文件变化
    /// </summary>
    /// <param name="path"></param>
    /// <param name="parameter"></param>
    private static void HandleFileChange(string path, OperationParameters parameter)
    {
        try
        {
            LogService.Logger.Info($"文件变化，开始执行操作：{parameter.SourcePath}");

            // 防止重复处理同一个文件
            if (!_processingFiles.TryAdd(path, 0))
            {
                LogService.Logger.Debug($"文件 {path} 正在处理，跳过重复事件。");
                return;
            }

            if (_sourceToTargetsCache.TryGetValue(parameter.SourcePath, out List<OperationParameters> operations))
            {
                foreach (var item in operations)
                {
                    var operationParams = new OperationParameters(
                        item.OperationMode,
                        path, // 使用文件变化的实际路径
                        Path.Combine(item.TargetPath, Path.GetFileName(path)),
                        item.FileOperationType,
                        item.HandleSubfolders,
                        item.Funcs,
                        item.PathFilter,
                        item.RuleModel
                    );

                    // 异步执行文件操作，确保文件存在
                    Task.Run(async () =>
                    {
                        if (parameter.RuleModel.RuleType == TaskRuleType.FileRule)
                        {
                            if (File.Exists(path))
                            {
                                await FileActuator.ExecuteFileOperationAsync(operationParams);
                            }
                            else
                            {
                                LogService.Logger.Debug($"文件 {path} 已不存在，无法执行操作。");
                            }
                        }
                        else
                        {
                            await FolderActuator.ExecuteFolderOperationAsync(parameter);
                        }          

                        // 操作完成后，从正在处理的集合中移除
                        _processingFiles.TryRemove(path, out _);
                    });
                }
            }
            else
            {
                var operationParams = OperationParameters.CreateOperationParameters(parameter);
                Task.Run(async () =>
                {
                    if (parameter.RuleModel.RuleType == TaskRuleType.FileRule)
                    {
                        if (File.Exists(path))
                        {
                            await FileActuator.ExecuteFileOperationAsync(operationParams);
                        }
                        else
                        {
                            LogService.Logger.Debug($"文件 {path} 已不存在，无法执行操作。");
                        }
                    }
                    else
                    {
                        await FolderActuator.ExecuteFolderOperationAsync(parameter);
                    }

                    // 操作完成后，从正在处理的集合中移除
                    _processingFiles.TryRemove(path, out _);

                    _processingFiles.TryRemove(path, out _);
                });
            }
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"文件变化处理失败：{ex.Message}");
            _processingFiles.TryRemove(path, out _); // 确保异常时移除
        }

    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public static void StopAllMonitoring()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
        LogService.Logger.Info("All monitoring has been stopped.");
    }

    /// <summary>
    /// 停止监控某个路径
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    public static void StopMonitor(string sourcePath, string targetPath)
    {
        if (_sourceToTargetsCache.TryGetValue(sourcePath, out var list))
        {
            // 尝试从列表中移除指定的 value
            var itemToRemove = list.FirstOrDefault(o => o.TargetPath == targetPath);

            if (itemToRemove != null)
            {
                list.Remove(itemToRemove);
                // 如果移除成功，检查该 key 是否还有其他的值
                if (list.Count == 0)
                {
                    // 如果列表为空，则移除该 key
                    _sourceToTargetsCache.TryRemove(sourcePath, out _);
                    _watchers.TryGetValue(sourcePath, out var watcher);
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    _watchers.TryRemove(sourcePath, out _);
                    LogService.Logger.Info($"停止监控：{sourcePath}");
                }
            }
        }
    }

}
