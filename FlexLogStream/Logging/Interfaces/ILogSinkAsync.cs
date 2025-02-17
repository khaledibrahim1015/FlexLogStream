using FlexLogStream.Logging.Enums;

namespace FlexLogStream.Logging.Interfaces;

/// <summary>
/// This interface will allow us to create different logging implementations (e.g., file, RabbitMQ) Synchronously
/// that adhere to the same contract.
/// ILogSinkAsync: This interface defines a contract for logging. Any logging sink (e.g., file, RabbitMQ) must implement this interface
/// </summary>
public interface ILogSinkAsync
{
    Task LogAsync(string message);
    Task LogAsync(string message, LogLevel level);
    Task LogAsync(string message, LogLevel logLevel, Exception exception = default);
}
