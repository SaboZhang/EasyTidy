using EasyTidy.Model;
using EasyTidy.Util;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EasyTidy.Service;

public static class FileEventHandler
{
    private static Dictionary<string, FileSystemWatcher> _watchers = [];

    /// <summary>
    /// 监控文件变化
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="targetPath"></param>
    /// <param name="delaySeconds"></param>
    /// <param name="fileOperationType"></param>
    public static void MonitorFolder(OperationParameters parameter, int delaySeconds = 5)
    {
        if (_watchers.ContainsKey(parameter.SourcePath)) return; // 防止重复监控

        var watcher = new FileSystemWatcher
        {
            Path = parameter.SourcePath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            Filter = "*.*"
        };

        watcher.Created += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(parameter));
        watcher.Deleted += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(parameter));
        watcher.Changed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(parameter));
        watcher.Renamed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(parameter));
        watcher.EnableRaisingEvents = true;

        _watchers[parameter.SourcePath] = watcher; // 存储监控器
    }

    private static void OnFileChange(FileSystemEventArgs e, int delaySeconds, Action action)
    {
        System.Threading.ThreadPool.QueueUserWorkItem(state =>
        {
            System.Threading.Thread.Sleep(delaySeconds * 1000);
            action();
        });
    }

    private static void HandleFileChange(OperationParameters parameter)
    {
        Task.Run(() => FileActuator.ExecuteFileOperationAsync(parameter));
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
