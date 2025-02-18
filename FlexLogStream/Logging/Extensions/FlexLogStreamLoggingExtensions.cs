using FlexLogStream.Logging.Configuration;
using FlexLogStream.Logging.Managers;
using FlexLogStream.Logging.Models;
using FlexLogStream.Logging.Sinks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlexLogStream.Logging.Extensions;


public static class FlexLogStreamLoggingExtensions
{
    public static IHost UseFlexLogStreamLogging(this IHost host, IConfiguration configuration)
    {

        // Load configuration from appsettings
        var rabbitMQConfig = configuration.GetSection("RabbitMQConfiguration").Get<RabbitMQCongiguration>();
        if (!rabbitMQConfig!.Enable)
            return host;

        // Initialize logging
        var loggingManager = new AsyncLoggingManager();
        loggingManager.AddSink(new FileLogSinkAsync(rabbitMQConfig.FallbackLogFilePath));
        loggingManager.AddSink(new RabbitMQLogSink(
            rabbitMQConfig.HostName,
            rabbitMQConfig.Port,
            rabbitMQConfig.UserName,
            rabbitMQConfig.Password,
            rabbitMQConfig.RabbitMqSettings.ExchangeName,
            rabbitMQConfig.RabbitMqSettings.QueueName,
            rabbitMQConfig.RabbitMqSettings.RoutingKey,
            rabbitMQConfig.FallbackLogFilePath
        ));

        // Override built-in logging
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        loggerFactory.AddProvider(new AsyncFlexLogStreamLoggingProvider(loggingManager));

        return host;
    }
}
