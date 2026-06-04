namespace AuthService.Models;

/// <summary>
/// User entity for authentication
/// </summary>
public class User
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;  // Plain text for MVP, use BCrypt in production
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;  // "Employee" or "Manager"
    public string? ManagerId { get; set; }  // Reference to manager (for employees)
    public List<string> TeamMemberIds { get; set; } = new();  // Direct reports (for managers)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
