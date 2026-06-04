namespace NotificationService.Models;

/// <summary>
/// Notification entity for tracking sent notifications
/// </summary>
public class Notification
{
    public string NotificationId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedLeaveId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Notification type constants
/// </summary>
public static class NotificationType
{
    public const string LeaveApplied = "LeaveApplied";
    public const string LeaveApproved = "LeaveApproved";
    public const string LeaveRejected = "LeaveRejected";
    public const string LeaveCancelled = "LeaveCancelled";
}
