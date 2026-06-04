namespace EmployeeService.Models;

/// <summary>
/// Response DTO for employee details
/// </summary>
public class EmployeeResponse
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public DateTime JoiningDate { get; set; }
}
