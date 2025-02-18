using FlexLogStream.Logging.Configuration;
using FlexLogStream.Logging.Managers;
using FlexLogStream.Logging.Models;
using FlexLogStream.Logging.Services;
using FlexLogStream.Logging.Sinks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlexLogStream.Logging.Extensions;


/// <summary>
/// Extension methods for setting up FlexLogStream logging in an <see cref="IHost"/>.
/// </summary>
public static class FlexLogStreamLoggingExtensions
{
    /// <summary>
    /// Configures the host to use FlexLogStream logging.
    /// </summary>
    /// <param name="host">The <see cref="IHost"/> to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The configured <see cref="IHost"/>.</returns>
    public static IHost UseFlexLogStreamLogging(this IHost host, IConfiguration configuration)
    {
        // Load configuration from appsettings
        var rabbitMQConfig = configuration.GetSection("RabbitMQConfiguration").Get<RabbitMQCongiguration>();
        if (!rabbitMQConfig!.Enable)
            return host;

        // Initialize logging
        var loggingManager = new AsyncLoggingManager();
        loggingManager.AddSink(new FileLogSinkAsync(rabbitMQConfig.FallbackLogFilePath));
        var rabbitMqLogSink = new RabbitMQLogSink(
            rabbitMQConfig.HostName,
            rabbitMQConfig.Port,
            rabbitMQConfig.UserName,
            rabbitMQConfig.Password,
            rabbitMQConfig.RabbitMqSettings.ExchangeName,
            rabbitMQConfig.RabbitMqSettings.QueueName,
            rabbitMQConfig.RabbitMqSettings.RoutingKey,
            rabbitMQConfig.FallbackLogFilePath
        );
        loggingManager.AddSink(rabbitMqLogSink);

        // Override built-in logging
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        loggerFactory.AddProvider(new AsyncFlexLogStreamLoggingProvider(loggingManager));



        // Register the log forwarder service
        //host.Services.AddHostedService(provider => new LogForwarderService(rabbitMqLogSink, rabbitMQConfig.FallbackLogFilePath));
        // Register the log forwarder service
        // Register the log forwarder service
        var services = new ServiceCollection();
        services.AddSingleton(rabbitMqLogSink);
        services.AddSingleton(new LogForwarderService(rabbitMqLogSink, rabbitMQConfig.FallbackLogFilePath));
        services.AddHostedService<LogForwarderService>();
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IHostedService>();

        return host;
    }
}
