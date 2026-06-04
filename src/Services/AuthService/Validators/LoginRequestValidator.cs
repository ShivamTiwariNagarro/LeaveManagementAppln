using AuthService.Models;

namespace AuthService.Validators;

/// <summary>
/// Validator for login requests
/// </summary>
public static class LoginRequestValidator
{
    public static (bool IsValid, string? ErrorMessage) Validate(LoginRequest request)
    {
        if (request == null)
        {
            return (false, "Request body is required");
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return (false, "Username is required");
        }

        if (request.Username.Length < 3)
        {
            return (false, "Username must be at least 3 characters");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, "Password is required");
        }

        if (request.Password.Length < 6)
        {
            return (false, "Password must be at least 6 characters");
        }

        return (true, null);
    }
}
