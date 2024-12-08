using System.Collections.ObjectModel;

namespace EasyTidy.Log;

public interface ILoggingService
{
    void AddLogEntry(string logMessage); // 记录日志消息

    ObservableCollection<string> GetLogMessages();
}
