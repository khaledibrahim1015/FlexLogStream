using FlexLogStream.Logging.Enums;
using FlexLogStream.Logging.Interfaces;

namespace FlexLogStream.Logging.Managers;

/// <summary>
///   LoggingManager: This class manages multiple logging sinks (e.g., file, RabbitMQ) and distributes logs to them.
///   Extensibility: You can easily add more sinks(e.g., database, Elasticsearch) by implementing the ILogSink interface.
/// </summary>
public class AsyncLoggingManager
{
    private readonly List<ILogSinkAsync> _sinks;
    public AsyncLoggingManager()
    {
        _sinks = new List<ILogSinkAsync>();
    }

    public void AddSink(ILogSinkAsync sink)
    {
        _sinks.Add(sink);
    }

    public async Task LogAsync(string message, LogLevel logLevel, Exception exception = default)
    {
        foreach (var sink in _sinks)
        {
            await sink.LogAsync(message, logLevel, exception);
        }
    }


}
