using FlexLogStream.Logging.Sinks;
using Microsoft.Extensions.Hosting;

namespace FlexLogStream.Logging.Services;

public class LogForwarderService : BackgroundService
{
    private readonly RabbitMQLogSink _rabbitMQLogSink;
    private readonly FileLogSinkAsync _fileLogSinkAsync;
    private readonly string _fallbackLogFilePath;
    public LogForwarderService(RabbitMQLogSink rabbitMQLogSink, string fallbackLogFilePath)
    {
        _rabbitMQLogSink = rabbitMQLogSink;
        _fallbackLogFilePath = fallbackLogFilePath;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {

            try
            {
                if (!_rabbitMQLogSink.IsConnectionActive())
                {
                    _rabbitMQLogSink.Reconnect();
                    if (!_rabbitMQLogSink.IsConnectionActive())
                    {
                        await _fileLogSinkAsync.LogAsync("Failed to connect to RabbitMQ. Fallback to file logging.", Enums.LogLevel.Error);
                    }
                }
                if (_rabbitMQLogSink.IsConnectionActive())
                {
                    await ForwardLogsFromFileToRabbitMqAsync(); // Forward logs from file to RabbitMQ
                }


            }
            catch (Exception ex)
            {

                await _fileLogSinkAsync.LogAsync("Error in RabbitMQ log forwarder service", Enums.LogLevel.Error, ex);

            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Check every minute

        }


    }

    // Todo : reimpelement it with file sizes so that we don't read the whole file at once
    private async Task ForwardLogsFromFileToRabbitMqAsync()
    {

        if (!File.Exists(_fallbackLogFilePath))
            return;

        string[] logEntries;
        lock (_fallbackLogFilePath)
        {
            logEntries = File.ReadAllLines(_fallbackLogFilePath); // Read logs from file
            File.WriteAllText(_fallbackLogFilePath, string.Empty); // Clear the file
        }

        foreach (var logEntry in logEntries)
        {
            await _rabbitMQLogSink.LogAsync(logEntry, Enums.LogLevel.Information); // Forward logs to RabbitMQ
        }




    }
}
