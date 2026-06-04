namespace Shared.Common.Constants;

/// <summary>
/// Leave type enumeration
/// </summary>
public enum LeaveType
{
    Casual = 0,
    Sick = 1,
    Privilege = 2
}

/// <summary>
/// Leave type helper methods and constants
/// </summary>
public static class LeaveTypes
{
    // String constants for leave types
    public const string Casual = "Casual";
    public const string Sick = "Sick";
    public const string Privilege = "Privilege";

    /// <summary>
    /// Default leave allocation for new employees
    /// </summary>
    public static readonly Dictionary<LeaveType, int> DefaultAllocation = new()
    {
        { LeaveType.Casual, 12 },
        { LeaveType.Sick, 10 },
        { LeaveType.Privilege, 15 }
    };

    /// <summary>
    /// Get all leave type names
    /// </summary>
    public static IEnumerable<string> GetAllNames()
    {
        return Enum.GetNames<LeaveType>();
    }

    /// <summary>
    /// Try to parse leave type from string
    /// </summary>
    public static bool TryParse(string value, out LeaveType leaveType)
    {
        return Enum.TryParse(value, ignoreCase: true, out leaveType);
    }

    /// <summary>
    /// Check if a string is a valid leave type
    /// </summary>
    public static bool IsValid(string value)
    {
        return TryParse(value, out _);
    }
}
