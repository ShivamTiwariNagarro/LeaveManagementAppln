namespace EmployeeService.Models;

/// <summary>
/// Response DTO for leave balance
/// </summary>
public class LeaveBalanceResponse
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public List<LeaveTypeBalance> Balances { get; set; } = new();
}

public class LeaveTypeBalance
{
    public string LeaveType { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int UsedDays { get; set; }
    public int PendingDays { get; set; }
    public int AvailableDays { get; set; }
}
