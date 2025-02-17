namespace FlexLogStream.Logging.Models;

public class RabbitMQCongiguration
{
    public string HostName { get; set; }
    public int Port { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string FallbackLogFilePath { get; set; }
    public RabbitMqSettings RabbitMqSettings { get; set; }


}
public class RabbitMqSettings
{
    public string ExchangeName { get; set; }
    public string QueueName { get; set; }
    public string RoutingKey { get; set; }
}