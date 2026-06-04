using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs;
using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationDataStore _dataStore;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(NotificationDataStore dataStore, ILogger<NotificationsController> logger)
    {
        _dataStore = dataStore;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's notifications
    /// </summary>
    [HttpGet]
    public IActionResult GetMyNotifications()
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var currentUserId = User.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting notifications for user: {UserId}", 
            correlationId, currentUserId);

        var notifications = _dataStore.GetByRecipientId(currentUserId);
        return Ok(ApiResponse<IEnumerable<Notification>>.Ok(notifications));
    }

    /// <summary>
    /// Get current user's unread notifications
    /// </summary>
    [HttpGet("unread")]
    public IActionResult GetMyUnreadNotifications()
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var currentUserId = User.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting unread notifications for user: {UserId}", 
            correlationId, currentUserId);

        var notifications = _dataStore.GetUnreadByRecipientId(currentUserId);
        var count = _dataStore.GetUnreadCount(currentUserId);

        return Ok(ApiResponse<object>.Ok(new
        {
            unreadCount = count,
            notifications
        }));
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPost("{notificationId}/read")]
    public IActionResult MarkAsRead(string notificationId)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var currentUserId = User.FindFirst("user_id")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Marking notification {NotificationId} as read", 
            correlationId, notificationId);

        // Verify the notification belongs to current user
        var notification = _dataStore.GetById(notificationId);
        if (notification == null)
        {
            return NotFound(ApiResponse<object>.Fail("Notification not found"));
        }

        if (notification.RecipientId != currentUserId)
        {
            return Forbid();
        }

        var success = _dataStore.MarkAsRead(notificationId);
        return Ok(ApiResponse<object>.Ok(new { marked = true }, "Notification marked as read"));
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPost("read-all")]
    public IActionResult MarkAllAsRead()
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var currentUserId = User.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        _logger.LogInformation("CorrelationId: {CorrelationId} - Marking all notifications as read for user: {UserId}", 
            correlationId, currentUserId);

        var count = _dataStore.MarkAllAsRead(currentUserId);
        return Ok(ApiResponse<object>.Ok(new { markedCount = count }, $"Marked {count} notifications as read"));
    }

    /// <summary>
    /// Get notification by ID
    /// </summary>
    [HttpGet("{notificationId}")]
    public IActionResult GetById(string notificationId)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var currentUserId = User.FindFirst("user_id")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting notification: {NotificationId}", 
            correlationId, notificationId);

        var notification = _dataStore.GetById(notificationId);
        if (notification == null)
        {
            return NotFound(ApiResponse<object>.Fail("Notification not found"));
        }

        // Users can only view their own notifications
        if (notification.RecipientId != currentUserId)
        {
            return Forbid();
        }

        return Ok(ApiResponse<Notification>.Ok(notification));
    }
}
