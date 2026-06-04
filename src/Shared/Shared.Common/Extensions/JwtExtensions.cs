using System.Security.Claims;

namespace Shared.Common.Extensions;

/// <summary>
/// Extension methods for JWT claims access
/// </summary>
public static class JwtExtensions
{
    /// <summary>
    /// Get user ID from claims
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? principal.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Get username from claims
    /// </summary>
    public static string? GetUsername(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value 
            ?? principal.FindFirst("name")?.Value;
    }

    /// <summary>
    /// Get user role from claims
    /// </summary>
    public static string? GetRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Role)?.Value 
            ?? principal.FindFirst("role")?.Value;
    }

    /// <summary>
    /// Get manager ID from claims (for employees)
    /// </summary>
    public static string? GetManagerId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("managerId")?.Value;
    }

    /// <summary>
    /// Check if user is a manager
    /// </summary>
    public static bool IsManager(this ClaimsPrincipal principal)
    {
        return principal.GetRole()?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Check if user is an employee
    /// </summary>
    public static bool IsEmployee(this ClaimsPrincipal principal)
    {
        return principal.GetRole()?.Equals("Employee", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Check if user can access a specific employee's data
    /// (Own data or manager accessing team member data)
    /// </summary>
    public static bool CanAccessEmployeeData(this ClaimsPrincipal principal, string employeeId, IEnumerable<string>? teamMemberIds = null)
    {
        var userId = principal.GetUserId();
        
        // User accessing their own data
        if (userId == employeeId)
            return true;

        // Manager accessing team member data
        if (principal.IsManager() && teamMemberIds?.Contains(employeeId) == true)
            return true;

        return false;
    }
}
