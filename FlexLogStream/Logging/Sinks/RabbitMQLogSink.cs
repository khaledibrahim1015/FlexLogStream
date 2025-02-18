using FlexLogStream.Logging.Enums;
using FlexLogStream.Logging.Interfaces;
using FlexLogStream.Logging.Models;
using RabbitMQ.Client;
using System.Text;

namespace FlexLogStream.Logging.Sinks;


/// <summary>
/// Implement a RabbitMQ logging sink that publishes logs to a RabbitMQ queue.
/// This sink will use a persistent connection to avoid opening and closing connections for each log message.
/// </summary>
public class RabbitMQLogSink : ILogSinkAsync, IDisposable, IAsyncDisposable
{
    private readonly string _hostName;
    private readonly int _port;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly string _routingKey;
    private readonly string _fallbackLogFilePath;
    private readonly FileLogSinkAsync _fileLogSinkAsync;

    private readonly RabbitMQCongiguration _rabbitMQConfiguration;

    private bool _isDisposed;
    private readonly object _lock = new();
    private IConnection? _connection;
    private IChannel? _channel;

    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 5;
    private const int InitialReconnectDelayMs = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQLogSink"/> class using a RabbitMQ configuration object.
    /// </summary>
    /// <param name="rabbitMQConfiguration">The RabbitMQ configuration object.</param>
    public RabbitMQLogSink(RabbitMQCongiguration rabbitMQConfiguration)
    {
        _rabbitMQConfiguration = rabbitMQConfiguration;
        _hostName = rabbitMQConfiguration.HostName;
        _port = rabbitMQConfiguration.Port;
        _userName = rabbitMQConfiguration.UserName;
        _password = rabbitMQConfiguration.Password;
        _exchangeName = rabbitMQConfiguration.RabbitMqSettings.ExchangeName;
        _queueName = rabbitMQConfiguration.RabbitMqSettings.QueueName;
        _routingKey = rabbitMQConfiguration.RabbitMqSettings.RoutingKey;
        _fallbackLogFilePath = rabbitMQConfiguration.FallbackLogFilePath;
        _fileLogSinkAsync = new FileLogSinkAsync(_fallbackLogFilePath);

        TryEstablishConnectionAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQLogSink"/> class using individual RabbitMQ parameters.
    /// </summary>
    /// <param name="hostName">The RabbitMQ host name.</param>
    /// <param name="port">The RabbitMQ port.</param>
    /// <param name="userName">The RabbitMQ user name.</param>
    /// <param name="password">The RabbitMQ password.</param>
    /// <param name="exchangeName">The RabbitMQ exchange name.</param>
    /// <param name="queueName">The RabbitMQ queue name.</param>
    /// <param name="routingKey">The RabbitMQ routing key.</param>
    /// <param name="fallbackLogFilePath">The fallback log file path.</param>
    public RabbitMQLogSink(
        string hostName, int port, string userName, string password,
        string exchangeName, string queueName, string routingKey,
        string fallbackLogFilePath)
    {
        _hostName = hostName;
        _port = port;
        _userName = userName;
        _password = password;
        _exchangeName = exchangeName;
        _queueName = queueName;
        _routingKey = routingKey;
        _fallbackLogFilePath = fallbackLogFilePath;
        _fileLogSinkAsync = new FileLogSinkAsync(_fallbackLogFilePath);

        TryEstablishConnectionAsync().GetAwaiter().GetResult();
    }
    public Task LogAsync(string message)
    {
        return LogAsync(message, LogLevel.Information);
    }

    public Task LogAsync(string message, LogLevel level)
    {
        return LogAsync(message, level, null);
    }

    /// <summary>
    /// Logs a message asynchronously to RabbitMQ.
    /// </summary>
    /// <param name="message">The log message.</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="exception">The exception (optional).</param>
    public async Task LogAsync(string message, LogLevel logLevel, Exception exception = null)
    {
        if (IsConnectionActive())
        {
            try
            {
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {message}";
                if (exception != null)
                    logEntry += $"{Environment.NewLine} {exception}";
                var body = Encoding.UTF8.GetBytes(logEntry);

                lock (_lock)
                {
                    _channel.BasicPublishAsync(
                        exchange: _exchangeName,
                        routingKey: _routingKey,
                        mandatory: true,
                        basicProperties: new BasicProperties
                        {
                            Persistent = true
                        },
                        body: body
                    ).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to publish log message to RabbitMQ: {ex.Message}");
                await _fileLogSinkAsync.LogAsync(message, logLevel, exception);
            }
        }
        else
        {
            Reconnect();
            if (!IsConnectionActive())
            {
                await _fileLogSinkAsync.LogAsync(message, logLevel, exception);
            }
        }
    }

    /// <summary>
    /// Checks if the RabbitMQ connection is active.
    /// </summary>
    /// <returns>True if the connection is active, otherwise false.</returns>
    public bool IsConnectionActive()
    {
        lock (_lock)
        {
            return _connection != null && _connection.IsOpen
                && _channel != null && _channel.IsOpen;
        }
    }

    /// <summary>
    /// Attempts to reconnect to RabbitMQ.
    /// </summary>
    public void Reconnect()
    {
        lock (_lock)
        {
            if (_isDisposed || _reconnectAttempts >= MaxReconnectAttempts)
            {
                Console.WriteLine("Reconnection attempts exhausted. Falling back to file logging.");
                return;
            }

            try
            {
                Console.WriteLine($"Attempting to reconnect to RabbitMQ (Attempt {_reconnectAttempts + 1})...");
                TryEstablishConnectionAsync();
                if (IsConnectionActive())
                {
                    Console.WriteLine("Reconnection successful.");
                    _reconnectAttempts = 0;
                }
                else
                {
                    Console.WriteLine("Reconnection failed. Retrying...");
                    _reconnectAttempts++;
                    Task.Delay(InitialReconnectDelayMs * _reconnectAttempts).Wait();
                    Reconnect();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Reconnection failed. Retrying...");
                _reconnectAttempts++;
                Task.Delay(InitialReconnectDelayMs * _reconnectAttempts).Wait();
                Reconnect();
            }
        }
    }

    /// <summary>
    /// Tries to establish a connection to RabbitMQ asynchronously.
    /// </summary>
    private async Task TryEstablishConnectionAsync()
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            if (_isDisposed) return;

            try
            {
                ConnectionFactory factory = new()
                {
                    HostName = _hostName,
                    Port = _port,
                    UserName = _userName,
                    Password = _password,
                    VirtualHost = "/",
                };
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to establish RabbitMQ connection: {ex.Message}");
                _connection?.Dispose();
                _channel?.Dispose();
                _connection = null;
                _channel = null;
            }
        }
    }

    /// <summary>
    /// Disposes the RabbitMQLogSink instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the RabbitMQLogSink instance.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from Dispose or the finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }

        _isDisposed = true;
    }

    /// <summary>
    /// Disposes the RabbitMQLogSink instance asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the RabbitMQLogSink instance asynchronously.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_channel != null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }


}
