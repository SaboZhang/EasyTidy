using CommunityToolkit.WinUI;
using EasyTidy.Log;
using EasyTidy.Model;
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
        _sourceToTargetsCache[parameter.SourcePath] = _sourceToTargetsCache.TryGetValue(parameter.SourcePath, out var targets)
            ? targets.Concat(new List<OperationParameters> { new OperationParameters (
                parameter.OperationMode,
                parameter.SourcePath,
                parameter.TargetPath,
                parameter.FileOperationType,
                parameter.HandleSubfolders,
                parameter.Funcs,
                parameter.PathFilter,
                parameter.RuleModel) { }}).ToList()
            : new List<OperationParameters> { new OperationParameters (
                parameter.OperationMode,
                parameter.SourcePath,
                parameter.TargetPath,
                parameter.FileOperationType,
                parameter.HandleSubfolders,
                parameter.Funcs,
                parameter.PathFilter,
                parameter.RuleModel) { }};

        if (_watchers.ContainsKey(parameter.SourcePath)) return; // 防止重复监控

        LogService.Logger.Info($"监控文件变化：{parameter.SourcePath}");

        var watcher = new FileSystemWatcher
        {
            Path = parameter.SourcePath.Equals("DesktopText".GetLocalized()) 
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) 
            : parameter.SourcePath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
            Filter = "*.*"
        };

        watcher.Created += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, parameter));
        watcher.Deleted += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, parameter));
        watcher.Changed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, parameter));
        watcher.Renamed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, parameter));
        watcher.EnableRaisingEvents = true;

        _watchers[parameter.SourcePath] = watcher; // 存储监控器
    }

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

    private static void HandleFileChange(string path, OperationParameters parameter)
    {
        try
        {
            LogService.Logger.Info($"文件变化，开始执行操作：{parameter.SourcePath}");

            // 防止重复处理同一个文件
            if (!_processingFiles.TryAdd(path, 0))
            {
                LogService.Logger.Info($"文件 {path} 正在处理，跳过重复事件。");
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
                        if (File.Exists(path))
                        {
                            await FileActuator.ExecuteFileOperationAsync(operationParams);
                        }
                        else
                        {
                            LogService.Logger.Warn($"文件 {path} 已不存在，无法执行操作。");
                        }

                        // 操作完成后，从正在处理的集合中移除
                        _processingFiles.TryRemove(path, out _);
                    });
                }
            }
            else
            {
                var operationParams = new OperationParameters(
                    parameter.OperationMode,
                    path,
                    parameter.TargetPath,
                    parameter.FileOperationType,
                    parameter.HandleSubfolders,
                    parameter.Funcs,
                    parameter.PathFilter,
                    parameter.RuleModel
                );

                Task.Run(async () =>
                {
                    if (File.Exists(path))
                    {
                        await FileActuator.ExecuteFileOperationAsync(operationParams);
                    }
                    else
                    {
                        LogService.Logger.Warn($"文件 {path} 已不存在，无法执行操作。");
                    }

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

    public static void StopAllMonitoring()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
    }

}
