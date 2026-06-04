namespace Shared.Common.Configuration;

/// <summary>
/// RabbitMQ connection settings loaded from appsettings.json
/// </summary>
public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    
    // Exchange and queue names
    public string ExchangeName { get; set; } = "leave_management_exchange";
    public string? QueueName { get; set; }
    
    // Exchange for leave events
    public string LeaveExchange { get; set; } = "leave-events-exchange";
}
