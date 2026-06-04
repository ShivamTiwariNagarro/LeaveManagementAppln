namespace EmployeeService.Models;

/// <summary>
/// Leave balance for each leave type
/// </summary>
public class LeaveBalance
{
    public string EmployeeId { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int UsedDays { get; set; }
    public int PendingDays { get; set; }
    public int Year { get; set; } = DateTime.UtcNow.Year;
    
    public int AvailableDays => TotalDays - UsedDays - PendingDays;
}
