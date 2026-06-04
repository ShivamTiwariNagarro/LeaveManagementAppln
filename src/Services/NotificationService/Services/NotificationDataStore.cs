using System.Collections.Concurrent;
using NotificationService.Models;

namespace NotificationService.Services;

/// <summary>
/// In-memory notification data store
/// </summary>
public class NotificationDataStore
{
    private readonly ConcurrentDictionary<string, Notification> _notifications = new();
    private int _counter = 0;

    public string GenerateId()
    {
        return $"NOTIF-{DateTime.UtcNow:yyyyMMdd}-{Interlocked.Increment(ref _counter):D4}";
    }

    public Notification Add(Notification notification)
    {
        if (string.IsNullOrEmpty(notification.NotificationId))
        {
            notification.NotificationId = GenerateId();
        }
        notification.CreatedAt = DateTime.UtcNow;
        _notifications.TryAdd(notification.NotificationId, notification);
        return notification;
    }

    public Notification? GetById(string notificationId)
    {
        _notifications.TryGetValue(notificationId, out var notification);
        return notification;
    }

    public IEnumerable<Notification> GetByRecipientId(string recipientId)
    {
        return _notifications.Values
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
    }

    public IEnumerable<Notification> GetUnreadByRecipientId(string recipientId)
    {
        return _notifications.Values
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
    }

    public bool MarkAsRead(string notificationId)
    {
        if (_notifications.TryGetValue(notificationId, out var notification))
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    public int MarkAllAsRead(string recipientId)
    {
        var unreadNotifications = _notifications.Values
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ToList();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        return unreadNotifications.Count;
    }

    public int GetUnreadCount(string recipientId)
    {
        return _notifications.Values.Count(n => n.RecipientId == recipientId && !n.IsRead);
    }

    public IEnumerable<Notification> GetAll()
    {
        return _notifications.Values.OrderByDescending(n => n.CreatedAt).ToList();
    }
}
