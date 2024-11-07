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
    private static ConcurrentDictionary<string, List<KeyValuePair<string, List<Func<string, bool>>>>> _sourceToTargetsCache;

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
        ? targets.Concat(new List<KeyValuePair<string, List<Func<string, bool>>>> { new KeyValuePair<string, List<Func<string, bool>>>(parameter.TargetPath, parameter.Funcs) }).ToList()
        : new List<KeyValuePair<string, List<Func<string, bool>>>> { new KeyValuePair<string, List<Func<string, bool>>>(parameter.TargetPath, parameter.Funcs) };

        if (_watchers.ContainsKey(parameter.SourcePath)) return; // 防止重复监控

        LogService.Logger.Info($"监控文件变化：{parameter.SourcePath}");

        var watcher = new FileSystemWatcher
        {
            Path = parameter.SourcePath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
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
                Task.Delay(delaySeconds * 1000).ConfigureAwait(false);
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
            if (_sourceToTargetsCache.TryGetValue(parameter.SourcePath, out List<KeyValuePair<string, List<Func<string, bool>>>> value))
            {
                foreach (var targetPath in value)
                {
                    parameter.TargetPath = targetPath.Key;
                    parameter.Funcs = targetPath.Value;
                    parameter.SourcePath = path;
                    Task.Run(() => FileActuator.ExecuteFileOperationAsync(parameter));
                }
            }
            else
            {
                parameter.SourcePath = path;
                Task.Run(() => FileActuator.ExecuteFileOperationAsync(parameter));
            }
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"文件变化处理失败：{ex.Message}");
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
