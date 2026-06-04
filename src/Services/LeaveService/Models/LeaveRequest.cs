namespace LeaveService.Models;

/// <summary>
/// Leave request entity
/// </summary>
public class LeaveRequest
{
    public string LeaveId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = LeaveStatus.Pending;
    public string? ManagerId { get; set; }
    public string? ManagerComments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
}

/// <summary>
/// Leave status constants
/// </summary>
public static class LeaveStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
    
    public static bool IsValid(string status)
    {
        return status == Pending || status == Approved || 
               status == Rejected || status == Cancelled;
    }
}
