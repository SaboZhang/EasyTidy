using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Contracts.Service;
using EasyTidy.Log;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace EasyTidy.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private ObservableCollection<string> _logs;

    private readonly ILoggingService _loggingService;

    private readonly IAppNotificationService _appNotificationService;

    [ObservableProperty]
    public AdvancedCollectionView _logsListACV;

    // 用于展示的日志列表
    public ObservableCollection<string> Logs
    {
        get { return _logs; }
        set
        {
            _logs = value;
            OnPropertyChanged(); // 通知UI更新
        }
    }

    public LogsViewModel()
    {
        _appNotificationService = App.GetService<IAppNotificationService>();
        _loggingService = App.GetService<ILoggingService>();
        // 初始化日志列表
        Logs = new ObservableCollection<string>();
    }

    [RelayCommand]
    private void OnPageLoaded()
    {
        try
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                Logs = _loggingService.GetLogMessages();
                LogsListACV = new AdvancedCollectionView(Logs, true);
                LogsListACV.SortDescriptions.Add(new SortDescription("", SortDirection.Descending));
            });
        }
        catch (Exception ex)
        {
            LogService.Logger.Error($"日志加载失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private void OnOpenLogsFolder()
    {
        var logPath = Path.Combine(Model.Constants.LogPath, Model.Constants.Version);
        Process.Start("explorer.exe", logPath);
    }

    [RelayCommand]
    private void OnClearLogs()
    {
        LogService.UnRegister();
        ClearDirectory(Model.Constants.LogPath);
        Logs.Clear();
        LogService.Register(_loggingService, Model.Constants.AppName, LogLevel.Info, Model.Constants.Version);
        _appNotificationService.Show(string.Format("AppNotificationPayloadInfo".GetLocalized(), "日志清除成功"));
    }

    private static void ClearDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                string folderName = string.Empty;
                // 删除目录下的所有文件
                foreach (string file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }

                // 删除目录下的所有子目录及其内容
                foreach (string directory in Directory.GetDirectories(path))
                {
                    // 获取当前路径的文件夹名称
                    folderName = Path.GetFileName(directory);
                    ClearDirectory(directory); // 递归清理子目录
                    Directory.Delete(directory); // 删除子目录
                }
                var logMessage = string.IsNullOrEmpty(folderName)
                    ? "日志清理成功"
                    : $"版本：{folderName} 的日志清理完成";
                LogService.Logger.Info(logMessage);
            }
            catch (Exception ex)
            {
                LogService.Logger.Error($"Error while cleaning directory '{path}': {ex.Message}");
            }
        }
        else
        {
            LogService.Logger.Warn($"Directory '{path}' does not exist.");
        }
    }
}
