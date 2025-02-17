using FlexLogStream.Logging.Enums;
using FlexLogStream.Logging.Interfaces;
using System.Text;

namespace FlexLogStream.Logging.Sinks;

/// <summary>
/// FileLogSinkAsync: This class implements the ILogSinkAsync interface and writes logs to a file.
/// Thread Safety: The lock keyword ensures that multiple threads don’t write to the file simultaneously, preventing corruption.
/// File Management: The directory is created if it doesn’t exist, and logs are appended to the file.
/// Note : implement a file logging sink that writes logs to a file.and in case  This will act as a fallback when RabbitMQ is unavailable.
/// </summary>
public class FileLogSinkAsync : ILogSinkAsync
{

    private readonly string _logFilePath;
    private readonly object _lock = new();

    public FileLogSinkAsync(string logFilePath)
    {
        _logFilePath
            = !string.IsNullOrWhiteSpace(logFilePath)
                                ? logFilePath :
                                throw new ArgumentNullException(nameof(logFilePath));

        if (!Directory.Exists(Path.GetDirectoryName(_logFilePath)!))
            Directory.CreateDirectory(_logFilePath);

    }
    public Task LogAsync(string message)
    {
        return LogAsync(message, LogLevel.Information);
    }

    public Task LogAsync(string message, LogLevel level)
    {
        return LogAsync(message, level, null);
    }
    public Task LogAsync(string message, LogLevel logLevel, Exception exception = null)
    {
        StringBuilder builder = new StringBuilder();

        lock (_lock)
        {
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {message}";
            builder.AppendLine(logEntry);
            if (exception != null)
                builder.AppendLine(exception.ToString());

            using (StreamWriter writer = new StreamWriter(_logFilePath, true))
                writer.WriteLine(builder.ToString());

        }
        return Task.CompletedTask;
    }


}
