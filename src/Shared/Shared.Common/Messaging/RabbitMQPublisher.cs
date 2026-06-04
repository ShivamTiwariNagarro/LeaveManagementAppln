using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Common.Configuration;
using System.Text;
using System.Text.Json;

namespace Shared.Common.Messaging;

/// <summary>
/// RabbitMQ message publisher for sending leave events to notification service.
/// 
/// Purpose:
/// - Publishes leave-related messages to RabbitMQ exchanges
/// - Used by LeaveService to notify about leave applications, approvals, rejections
/// - NotificationService consumes these messages asynchronously
/// 
/// Pattern:
/// - Uses direct exchange for routing to specific queues
/// - Messages are JSON-serialized DTOs
/// - Connection is reused for multiple publishes
/// </summary>
public class RabbitMQPublisher : IDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed;

    public RabbitMQPublisher(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        InitializeRabbitMQ();
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange for leave events
            _channel.ExchangeDeclare(
                exchange: _settings.LeaveExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ publisher initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ publisher");
            throw;
        }
    }

    /// <summary>
    /// Publishes a message to the specified routing key
    /// </summary>
    public void Publish<T>(T message, string routingKey, string? correlationId = null)
    {
        if (_channel == null || !_channel.IsOpen)
        {
            _logger.LogWarning("RabbitMQ channel is not open. Attempting to reconnect...");
            InitializeRabbitMQ();
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel!.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            
            if (!string.IsNullOrEmpty(correlationId))
            {
                properties.CorrelationId = correlationId;
            }

            _channel.BasicPublish(
                exchange: _settings.LeaveExchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published message to exchange {Exchange} with routing key {RoutingKey}. CorrelationId: {CorrelationId}",
                _settings.LeaveExchange, routingKey, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to RabbitMQ");
            throw;
        }
    }

    /// <summary>
    /// Publish leave applied event
    /// </summary>
    public void PublishLeaveApplied<T>(T message, string? correlationId = null)
    {
        Publish(message, "leave.applied", correlationId);
    }

    /// <summary>
    /// Publish leave approved event
    /// </summary>
    public void PublishLeaveApproved<T>(T message, string? correlationId = null)
    {
        Publish(message, "leave.approved", correlationId);
    }

    /// <summary>
    /// Publish leave rejected event
    /// </summary>
    public void PublishLeaveRejected<T>(T message, string? correlationId = null)
    {
        Publish(message, "leave.rejected", correlationId);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        _disposed = true;
    }
}
