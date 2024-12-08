using EasyTidy.Log;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace EasyTidy.Service;

public class LoggingService : ILoggingService
{
    private readonly ObservableCollection<string> _logMessages;

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public LoggingService()
    {
        _logMessages = new ObservableCollection<string>();
    }

    // 将日志消息添加到列表中
    public async void AddLogEntry(string logMessage)
    {
        await Task.Run(() =>
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                _logMessages.Add(logMessage);

                // 保持日志条数不超过某个值，避免内存占用过多
                if (_logMessages.Count > 100)
                {
                    _logMessages.RemoveAt(0);
                }
            });
        });
    }

    // 获取当前所有日志条目
    public ObservableCollection<string> GetLogMessages()
    {
        return _logMessages;
    }
}
