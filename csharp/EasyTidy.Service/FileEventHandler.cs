using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Service;

public class FileEventHandler
{
    /// <summary>
    /// 监控文件变化
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="targetPath"></param>
    /// <param name="delaySeconds"></param>
    /// <param name="fileOperationType"></param>
    public static void MonitorFolder(string folderPath, string targetPath, int delaySeconds, FileOperationType fileOperationType)
    {
        FileSystemWatcher watcher = new()
        {
            Path = folderPath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            Filter = "*.*"
        };

        watcher.Created += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, targetPath, fileOperationType));
        watcher.Deleted += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, targetPath, fileOperationType));
        watcher.Changed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, targetPath, fileOperationType));
        watcher.Renamed += (sender, e) => OnFileChange(e, delaySeconds, () => HandleFileChange(e.FullPath, targetPath, fileOperationType));
        watcher.EnableRaisingEvents = true;

    }

    private static void OnFileChange(FileSystemEventArgs e, int delaySeconds, Action action)
    {
        System.Threading.ThreadPool.QueueUserWorkItem(state =>
        {
            System.Threading.Thread.Sleep(delaySeconds * 1000);
            action();
        });
    }

    private static void HandleFileChange(string source, string target, FileOperationType fileOperationType)
    {
        FileActuator.OnExecuteMoveFile(source, target, fileOperationType);
    }
}
