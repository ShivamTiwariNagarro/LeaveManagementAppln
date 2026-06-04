using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Common.Configuration;
using Shared.Common.Messages;
using NotificationService.Models;

namespace NotificationService.Services;

/// <summary>
/// Background service that consumes messages from RabbitMQ
/// </summary>
public class RabbitMQConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly RabbitMQSettings _settings;
    private readonly INotificationSender _notificationSender;
    private readonly NotificationDataStore _dataStore;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQConsumerService(
        ILogger<RabbitMQConsumerService> logger,
        IOptions<RabbitMQSettings> settings,
        INotificationSender notificationSender,
        NotificationDataStore dataStore)
    {
        _logger = logger;
        _settings = settings.Value;
        _notificationSender = notificationSender;
        _dataStore = dataStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Service starting...");
        
        await Task.Delay(2000, stoppingToken); // Wait for RabbitMQ to be ready
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    InitializeRabbitMQ();
                }
                
                await Task.Delay(5000, stoppingToken); // Keep alive check
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ connection failed. Retrying in 10 seconds...");
                await Task.Delay(10000, stoppingToken);
            }
        }
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

            // Declare exchange (must match publisher's exchange)
            _channel.ExchangeDeclare(
                exchange: _settings.LeaveExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Declare queue
            var queueName = _settings.QueueName ?? "notification_queue";
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind queue to exchange with routing keys
            _channel.QueueBind(queueName, _settings.LeaveExchange, "leave.applied");
            _channel.QueueBind(queueName, _settings.LeaveExchange, "leave.approved");
            _channel.QueueBind(queueName, _settings.LeaveExchange, "leave.rejected");
            _channel.QueueBind(queueName, _settings.LeaveExchange, "leave.cancelled");

            _logger.LogInformation("Connected to RabbitMQ. Listening on queue: {Queue}", queueName);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;

                    _logger.LogInformation("Received message with routing key: {RoutingKey}", routingKey);

                    await ProcessMessageAsync(routingKey, message);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue
                }
            };

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
            throw;
        }
    }

    private async Task ProcessMessageAsync(string routingKey, string messageJson)
    {
        switch (routingKey)
        {
            case "leave.applied":
                await HandleLeaveAppliedAsync(messageJson);
                break;
            case "leave.approved":
                await HandleLeaveApprovedAsync(messageJson);
                break;
            case "leave.rejected":
                await HandleLeaveRejectedAsync(messageJson);
                break;
            case "leave.cancelled":
                await HandleLeaveCancelledAsync(messageJson);
                break;
            default:
                _logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
                break;
        }
    }

    private async Task HandleLeaveAppliedAsync(string messageJson)
    {
        var message = JsonSerializer.Deserialize<LeaveAppliedMessage>(messageJson, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (message == null)
        {
            _logger.LogWarning("Failed to deserialize LeaveAppliedMessage");
            return;
        }

        _logger.LogInformation("Processing LeaveApplied for {EmployeeName}, LeaveId: {LeaveId}", 
            message.EmployeeName, message.LeaveId);

        // Notify Manager about new leave request
        if (!string.IsNullOrEmpty(message.ManagerId))
        {
            var subject = $"New Leave Request from {message.EmployeeName}";
            var body = $"{message.EmployeeName} has applied for {message.LeaveType} leave " +
                       $"from {message.StartDate:yyyy-MM-dd} to {message.EndDate:yyyy-MM-dd} " +
                       $"({message.NumberOfDays} days). Reason: {message.Reason}";

            await _notificationSender.SendAsync(message.ManagerId, "Manager", subject, body);

            // Store notification
            _dataStore.Add(new Notification
            {
                Type = NotificationType.LeaveApplied,
                RecipientId = message.ManagerId,
                RecipientName = "Manager",
                Subject = subject,
                Message = body,
                RelatedLeaveId = message.LeaveId
            });
        }

        // Confirm to Employee
        var employeeSubject = "Leave Application Submitted";
        var employeeBody = $"Your {message.LeaveType} leave request from {message.StartDate:yyyy-MM-dd} " +
                          $"to {message.EndDate:yyyy-MM-dd} has been submitted and is pending approval.";

        await _notificationSender.SendAsync(message.EmployeeId, message.EmployeeName, employeeSubject, employeeBody);

        _dataStore.Add(new Notification
        {
            Type = NotificationType.LeaveApplied,
            RecipientId = message.EmployeeId,
            RecipientName = message.EmployeeName,
            Subject = employeeSubject,
            Message = employeeBody,
            RelatedLeaveId = message.LeaveId
        });
    }

    private async Task HandleLeaveApprovedAsync(string messageJson)
    {
        var message = JsonSerializer.Deserialize<LeaveApprovedMessage>(messageJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (message == null)
        {
            _logger.LogWarning("Failed to deserialize LeaveApprovedMessage");
            return;
        }

        _logger.LogInformation("Processing LeaveApproved for {EmployeeName}, LeaveId: {LeaveId}",
            message.EmployeeName, message.LeaveId);

        var subject = "Leave Request Approved! ✅";
        var body = $"Great news! Your {message.LeaveType} leave request from {message.StartDate:yyyy-MM-dd} " +
                   $"to {message.EndDate:yyyy-MM-dd} ({message.NumberOfDays} days) has been approved.";

        if (!string.IsNullOrEmpty(message.ManagerComments))
        {
            body += $" Manager's comments: {message.ManagerComments}";
        }

        await _notificationSender.SendAsync(message.EmployeeId, message.EmployeeName, subject, body);

        _dataStore.Add(new Notification
        {
            Type = NotificationType.LeaveApproved,
            RecipientId = message.EmployeeId,
            RecipientName = message.EmployeeName,
            Subject = subject,
            Message = body,
            RelatedLeaveId = message.LeaveId
        });
    }

    private async Task HandleLeaveRejectedAsync(string messageJson)
    {
        var message = JsonSerializer.Deserialize<LeaveRejectedMessage>(messageJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (message == null)
        {
            _logger.LogWarning("Failed to deserialize LeaveRejectedMessage");
            return;
        }

        _logger.LogInformation("Processing LeaveRejected for {EmployeeName}, LeaveId: {LeaveId}",
            message.EmployeeName, message.LeaveId);

        var subject = "Leave Request Rejected ❌";
        var body = $"Your {message.LeaveType} leave request from {message.StartDate:yyyy-MM-dd} " +
                   $"to {message.EndDate:yyyy-MM-dd} ({message.NumberOfDays} days) has been rejected. " +
                   $"Reason: {message.RejectionReason}";

        await _notificationSender.SendAsync(message.EmployeeId, message.EmployeeName, subject, body);

        _dataStore.Add(new Notification
        {
            Type = NotificationType.LeaveRejected,
            RecipientId = message.EmployeeId,
            RecipientName = message.EmployeeName,
            Subject = subject,
            Message = body,
            RelatedLeaveId = message.LeaveId
        });
    }

    private async Task HandleLeaveCancelledAsync(string messageJson)
    {
        var message = JsonSerializer.Deserialize<LeaveCancelledMessage>(messageJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (message == null)
        {
            _logger.LogWarning("Failed to deserialize LeaveCancelledMessage");
            return;
        }

        _logger.LogInformation("Processing LeaveCancelled for {EmployeeName}, LeaveId: {LeaveId}",
            message.EmployeeName, message.LeaveId);

        // Notify Manager about cancellation
        if (!string.IsNullOrEmpty(message.ManagerId))
        {
            var managerSubject = $"Leave Request Cancelled by {message.EmployeeName}";
            var managerBody = $"{message.EmployeeName} has cancelled their {message.LeaveType} leave request " +
                             $"from {message.StartDate:yyyy-MM-dd} to {message.EndDate:yyyy-MM-dd} ({message.NumberOfDays} days).";

            await _notificationSender.SendAsync(message.ManagerId, "Manager", managerSubject, managerBody);

            _dataStore.Add(new Notification
            {
                Type = NotificationType.LeaveCancelled,
                RecipientId = message.ManagerId,
                RecipientName = "Manager",
                Subject = managerSubject,
                Message = managerBody,
                RelatedLeaveId = message.LeaveId
            });
        }

        // Confirm to Employee
        var employeeSubject = "Leave Request Cancelled";
        var employeeBody = $"Your {message.LeaveType} leave request from {message.StartDate:yyyy-MM-dd} " +
                          $"to {message.EndDate:yyyy-MM-dd} ({message.NumberOfDays} days) has been successfully cancelled.";

        await _notificationSender.SendAsync(message.EmployeeId, message.EmployeeName, employeeSubject, employeeBody);

        _dataStore.Add(new Notification
        {
            Type = NotificationType.LeaveCancelled,
            RecipientId = message.EmployeeId,
            RecipientName = message.EmployeeName,
            Subject = employeeSubject,
            Message = employeeBody,
            RelatedLeaveId = message.LeaveId
        });
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
