namespace Shared.Common.Constants;

/// <summary>
/// User role enumeration
/// </summary>
public enum UserRole
{
    Employee = 0,
    Manager = 1
}

/// <summary>
/// Role constants and helper methods
/// </summary>
public static class Roles
{
    public const string Employee = "Employee";
    public const string Manager = "Manager";

    /// <summary>
    /// Get all role names
    /// </summary>
    public static IEnumerable<string> GetAllRoles()
    {
        return new[] { Employee, Manager };
    }

    /// <summary>
    /// Check if a role string is valid
    /// </summary>
    public static bool IsValid(string role)
    {
        return role == Employee || role == Manager;
    }

    /// <summary>
    /// Check if role is manager
    /// </summary>
    public static bool IsManager(string role)
    {
        return role == Manager;
    }

    /// <summary>
    /// Check if role is employee
    /// </summary>
    public static bool IsEmployee(string role)
    {
        return role == Employee;
    }
}
