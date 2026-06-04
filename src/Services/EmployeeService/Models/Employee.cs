namespace EmployeeService.Models;

/// <summary>
/// Employee entity matching AuthService users
/// </summary>
public class Employee
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public DateTime JoiningDate { get; set; }
    public bool IsActive { get; set; } = true;
    
    public string FullName => $"{FirstName} {LastName}";
}
