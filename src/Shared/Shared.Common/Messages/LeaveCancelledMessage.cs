namespace Shared.Common.Messages;

/// <summary>
/// Message published when a leave request is cancelled by the employee
/// </summary>
public class LeaveCancelledMessage
{
    public string LeaveId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string ManagerId { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfDays { get; set; }
    public DateTime CancelledAt { get; set; }
    public string? CorrelationId { get; set; }
}
