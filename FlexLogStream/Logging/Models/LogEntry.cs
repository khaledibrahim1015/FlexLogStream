using FlexLogStream.Logging.Enums;

namespace FlexLogStream.Logging.Models;

public class LogEntry
{
    public string Message { get; set; }
    public LogLevel LogLevel { get; set; }
    public Exception Exception { get; set; }

    public LogEntry(string message, LogLevel logLevel, Exception exception = default)
    {
        Message = message;
        LogLevel = logLevel;
        Exception = exception;
    }

    public override string ToString()
    {
        return $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{LogLevel}] {Message}";
    }

}
