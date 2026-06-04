using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Common.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shared.Common.Utilities;

/// <summary>
/// Utility class for generating JWT tokens for authenticated users.
/// 
/// Purpose:
/// - Creates signed JWT tokens after successful user login
/// - Embeds user identity and authorization claims in the token
/// - Used by AuthService /login endpoint
/// 
/// Token structure:
/// - Header: Algorithm (HS256) and token type (JWT)
/// - Payload: Claims (sub, name, user_role, managerId, jti, exp, iss, aud)
/// - Signature: HMAC-SHA256 using SecretKey from configuration
/// 
/// Security notes:
/// - SecretKey must be at least 32 characters for HS256
/// - Tokens are self-contained (no server-side session storage needed)
/// - Tokens expire after configured time (default 60 minutes)
/// - Invalid/expired tokens are rejected by authentication middleware
/// </summary>
public class JwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Generates a JWT token for an authenticated user.
    /// </summary>
    /// <param name="userId">Unique user identifier</param>
    /// <param name="username">User's username</param>
    /// <param name="fullName">User's display name</param>
    /// <param name="role">User's role for authorization ("Employee" or "Manager")</param>
    /// <param name="managerId">Manager's user ID (for employees only)</param>
    /// <returns>Signed JWT token as base64-encoded string</returns>
    public string GenerateToken(string userId, string username, string fullName, string role, string? managerId = null)
    {
        // Create signing credentials using HMAC-SHA256 algorithm
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Define claims to embed in the token
        // Using "user_role" instead of "role" to avoid ASP.NET Core claim transformations
        // This ensures Ocelot's RouteClaimsRequirement works correctly
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),           // Subject: User ID
            new Claim("user_id", userId),                             // User ID (explicit for service extraction)
            new Claim(JwtRegisteredClaimNames.Name, fullName),        // Display name
            new Claim("username", username),                          // Username for login
            new Claim("user_role", role),                             // Custom role claim (Ocelot compatible)
            new Claim(ClaimTypes.Role, role),                         // Standard role claim for [Authorize(Roles=)]
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // JWT ID: Unique token identifier
        };

        // Add managerId claim for employees (used for authorization checks)
        if (!string.IsNullOrEmpty(managerId))
        {
            claims.Add(new Claim("manager_id", managerId));
        }

        // Create the JWT token with all components
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,                      // Who created the token
            audience: _jwtSettings.Audience,                  // Who the token is for
            claims: claims,                                   // User identity and authorization data
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes), // Token validity period
            signingCredentials: credentials                   // Signature to prevent tampering
        );

        // Serialize token to string format (header.payload.signature)
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
