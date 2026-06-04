using AuthService.Data;
using AuthService.Models;
using Microsoft.Extensions.Options;
using Shared.Common.Configuration;
using Shared.Common.Utilities;

namespace AuthService.Services;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthServiceImpl : IAuthService
{
    private readonly UserDataStore _userDataStore;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthServiceImpl> _logger;

    public AuthServiceImpl(
        UserDataStore userDataStore,
        JwtTokenGenerator tokenGenerator,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthServiceImpl> logger)
    {
        _userDataStore = userDataStore;
        _tokenGenerator = tokenGenerator;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public LoginResponse? Authenticate(string username, string password)
    {
        _logger.LogInformation("Login attempt for username: {Username}", username);

        var user = _userDataStore.GetByUsername(username);
        
        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for username: {Username}", username);
            return null;
        }

        // In production, use BCrypt.Verify() instead of plain text comparison
        if (user.Password != password)
        {
            _logger.LogWarning("Login failed: Invalid password for username: {Username}", username);
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User is inactive: {Username}", username);
            return null;
        }

        // Generate JWT token
        var token = _tokenGenerator.GenerateToken(
            user.UserId,
            user.Username,
            user.FullName,
            user.Role,
            user.ManagerId);

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        _logger.LogInformation("Login successful for user: {UserId} ({Username})", user.UserId, user.Username);

        return new LoginResponse
        {
            Token = token,
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            ExpiresAt = expiresAt
        };
    }

    public User? ValidateUser(string userId)
    {
        return _userDataStore.GetById(userId);
    }
}
