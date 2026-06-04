namespace AuthService.Models;

/// <summary>
/// Login request DTO
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
