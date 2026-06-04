using AuthService.Models;

namespace AuthService.Services;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Login response with JWT token, or null if authentication fails</returns>
    LoginResponse? Authenticate(string username, string password);

    /// <summary>
    /// Validate JWT token and return user info
    /// </summary>
    /// <param name="userId">User ID from token claims</param>
    /// <returns>User info if valid</returns>
    User? ValidateUser(string userId);
}
