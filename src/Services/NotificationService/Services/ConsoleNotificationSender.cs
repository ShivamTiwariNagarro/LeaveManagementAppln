namespace NotificationService.Services;

/// <summary>
/// Console-based notification sender for demo purposes
/// In production, this would be replaced with Email/SMS/Push notification service
/// </summary>
public class ConsoleNotificationSender : INotificationSender
{
    private readonly ILogger<ConsoleNotificationSender> _logger;

    public ConsoleNotificationSender(ILogger<ConsoleNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string recipientId, string recipientName, string subject, string message)
    {
        // In production, this would send actual email/SMS/push notifications
        // For demo, we just log to console with a nice format
        
        _logger.LogInformation("========================================");
        _logger.LogInformation("📧 NOTIFICATION SENT");
        _logger.LogInformation("========================================");
        _logger.LogInformation("To: {RecipientName} ({RecipientId})", recipientName, recipientId);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Message: {Message}", message);
        _logger.LogInformation("========================================");
        
        return Task.CompletedTask;
    }
}
