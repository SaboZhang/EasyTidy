using CommunityToolkit.WinUI;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;

namespace EasyTidy.Log;

public class LogEntrySink : ILogEventSink
{

    private readonly ILoggingService _loggingService;

    public LogEntrySink(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public void Emit(LogEvent logEvent)
    {
        // 获取时间戳并格式化
        var formattedTimestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");

        // 获取日志级别
        var localizedLogLevel = GetLocalizedLogLevel(logEvent.Level);

        // 获取格式化的日志消息
        var formattedMessage = logEvent.RenderMessage();
        // 这里将日志传递到日志服务，或存储在内存中供UI使用
        _loggingService?.AddLogEntry($"{formattedTimestamp} [{localizedLogLevel}] {formattedMessage}");
    }

    private static readonly Dictionary<LogEventLevel, string> LogLevelTranslations = new()
    {
        { LogEventLevel.Verbose, "Verbose" },
        { LogEventLevel.Debug, "Debug" },
        { LogEventLevel.Information, "Information" },
        { LogEventLevel.Warning, "Warning" },
        { LogEventLevel.Error, "Error" },
        { LogEventLevel.Fatal, "Fatal" }
    };

    private static string GetLocalizedLogLevel(LogEventLevel level)
    {
        return LogLevelTranslations.TryGetValue(level, out var localizedLevel)
            ? localizedLevel.GetLocalized().ToUpper()
            : level.ToString().ToUpper();  // 默认返回英文级别
    }

}
