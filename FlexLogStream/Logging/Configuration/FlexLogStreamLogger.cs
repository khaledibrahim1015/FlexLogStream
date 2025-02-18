using FlexLogStream.Logging.Managers;
using Microsoft.Extensions.Logging;

namespace FlexLogStream.Logging.Configuration;

public class FlexLogStreamLogger : ILogger
{
    private readonly AsyncLoggingManager _asyncLoggingManager;
    private readonly string _categoryName;

    public FlexLogStreamLogger(AsyncLoggingManager asyncLoggingManager, string categoryName)
    {
        _asyncLoggingManager = asyncLoggingManager;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;

    // Enable all log levels

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
         => true;
    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var customLogLevel = MapLogLevel(logLevel);

        _asyncLoggingManager.LogAsync(message, customLogLevel, exception).Wait();
    }


    private static Enums.LogLevel MapLogLevel(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        return logLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => Enums.LogLevel.Information,
            Microsoft.Extensions.Logging.LogLevel.Debug => Enums.LogLevel.Information,
            Microsoft.Extensions.Logging.LogLevel.Information => Enums.LogLevel.Information,
            Microsoft.Extensions.Logging.LogLevel.Warning => Enums.LogLevel.Warning,
            Microsoft.Extensions.Logging.LogLevel.Error => Enums.LogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => Enums.LogLevel.Critical,
            _ => Enums.LogLevel.Information
        };
    }




}
