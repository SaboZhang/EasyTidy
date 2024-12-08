

namespace EasyTidy.Log;
public class LogService
{
#if true

    public static void Register(ILoggingService loggingService, string name = "", LogLevel minLevel = LogLevel.Debug, string version = "1.0.0.0")
    {
        _logger = name.ToLower() switch
        {
            "serilog" => new SerilogLogger(loggingService, minLevel, version),
            _ => new SerilogLogger(loggingService, minLevel, version)
        };
    }

    public static void UnRegister()
    {
        _logger?.Dispose();
    }

    private static ILogger? _logger;

    public static ILogger Logger
    {
        get => _logger!;
        set => _logger = value;
    }

#else
        private static readonly Lazy<ILogger> _logger = new(() => new SerilogLogger());
        public static ILogger Logger => _logger.Value;

        public static void UnRegister()
        {
            _logger.Value.Dispose();
        }
#endif
}
