using FlexLogStream.Logging.Enums;
using FlexLogStream.Logging.Interfaces;
using System.Text;

namespace FlexLogStream.Logging.Sinks;

/// <summary>
/// FileLogSink: This class implements the ILogSink interface and writes logs to a file.
/// Thread Safety: The lock keyword ensures that multiple threads don’t write to the file simultaneously, preventing corruption.
/// File Management: The directory is created if it doesn’t exist, and logs are appended to the file.
/// Note : implement a file logging sink that writes logs to a file.and in case  This will act as a fallback when RabbitMQ is unavailable.
/// </summary>
public class FileLogSink : ILogSink
{
    private readonly string _logFlePath;
    private readonly object _lock = new();

    public FileLogSink(string logFilePath)
    {
        _logFlePath = !string.IsNullOrWhiteSpace(logFilePath)
                                ? logFilePath :
                                throw new ArgumentNullException(nameof(logFilePath));
        // Ensure the directory exists before writing to the file
        if (!Directory.Exists(Path.GetDirectoryName(_logFlePath)!))
            Directory.CreateDirectory(_logFlePath);
    }


    public void Log(string message)
    {
        Log(message, LogLevel.Information);
    }

    public void Log(string message, LogLevel level)
    {
        Log(message, level, null);
    }

    public void Log(string message, LogLevel logLevel, Exception exception = null)
    {
        StringBuilder sb = new StringBuilder();
        lock (_lock)
        {
            var LogEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {message}";
            sb.AppendLine(LogEntry);
            if (exception != null)
                sb.AppendLine(exception.ToString());

            using StreamWriter writer = new StreamWriter(_logFlePath, true);
            writer.WriteLine(sb.ToString());

        }

    }

}
