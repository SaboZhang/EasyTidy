using EasyTidy.Model;
using EasyTidy.Util;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EasyTidy.Service;

public static class FileEventHandler
{
    private static Dictionary<string, FileSystemWatcher> _watchers = [];
    private static readonly char[] separator = [';', '|'];

    /// <summary>
    /// 监控文件变化
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="targetPath"></param>
    /// <param name="delaySeconds"></param>
    /// <param name="fileOperationType"></param>
    public static void MonitorFolder(OperationMode operationMode, string folderPath, string targetPath, int delaySeconds, RuleModel filter, bool subFolder = true, FileOperationType fileOperationType = FileOperationType.Skip)
    {
        if (_watchers.ContainsKey(folderPath)) return; // 防止重复监控

        var watcher = new FileSystemWatcher
        {
            Path = folderPath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            Filter = "*.*"
        };

        watcher.Created += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter, subFolder));
        watcher.Deleted += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter, subFolder));
        watcher.Changed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter, subFolder));
        watcher.Renamed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(operationMode, e.FullPath, targetPath, fileOperationType, filter, subFolder));
        watcher.EnableRaisingEvents = true;

        _watchers[folderPath] = watcher; // 存储监控器
    }

    private static void OnFileChange(FileSystemEventArgs e, int delaySeconds, Action action)
    {
        System.Threading.ThreadPool.QueueUserWorkItem(state =>
        {
            System.Threading.Thread.Sleep(delaySeconds * 1000);
            action();
        });
    }

    private static void HandleFileChange(OperationMode operationMode, string source, string target, FileOperationType fileOperationType, RuleModel filter, bool subFolder = true)
    {
        // 定义过滤规则
        List<Func<string, bool>> pathFilters = FilterUtil.GetPathFilters(filter.Filter);
        // 根据 rule 和 RuleType 动态生成的过滤条件
        List<Func<string, bool>> dynamicFilters = FilterUtil.GeneratePathFilters(filter.Rule, filter.RuleType);
        // 合并两组过滤条件
        pathFilters.AddRange(dynamicFilters);
        // FileActuator.OnExecuteMoveFile(source, target, fileOperationType);FileActuator.OnExecuteMoveFile(source, target, fileOperationType);
        FileActuator.ExecuteFileOperation(operationMode, source, target, fileOperationType, subFolder, pathFilters);
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
