using FlexLogStream.Logging.Enums;
using FlexLogStream.Logging.Interfaces;

namespace FlexLogStream.Logging.Managers;

public class LoggingManager
{
    private readonly List<ILogSink> _sinks;
    public LoggingManager()
    {
        _sinks = new List<ILogSink>();
    }
    public void AddSink(ILogSink sink)
    {
        _sinks.Add(sink);
    }
    public void Log(string message, LogLevel logLevel, Exception exception = default)
    {
        foreach (var sink in _sinks)
        {
            sink.Log(message, logLevel, exception);
        }
    }
}
