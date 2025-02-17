using FlexLogStream.Logging.Enums;

namespace FlexLogStream.Logging.Interfaces;

/// <summary>
/// This interface will allow us to create different logging implementations (e.g., file, RabbitMQ) Asynchronously
/// that adhere to the same contract.
/// ILogSinkAsync: This interface defines a contract for logging. Any logging sink (e.g., file, RabbitMQ) must implement this interface
/// </summary>
public interface ILogSink
{
    void Log(string message);
    void Log(string message, LogLevel level);
    void Log(string message, LogLevel logLevel, Exception exception = default);
}
