namespace NotificationService.Services;

/// <summary>
/// Interface for sending notifications
/// </summary>
public interface INotificationSender
{
    Task SendAsync(string recipientId, string recipientName, string subject, string message);
}
