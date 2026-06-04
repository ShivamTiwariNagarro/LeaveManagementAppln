namespace Shared.Common.Messages;

/// <summary>
/// Message published when a leave application is submitted
/// </summary>
public class LeaveAppliedMessage
{
    public string LeaveId { get; set; } = string.Empty;
    public Guid LeaveRequestId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public string? CorrelationId { get; set; }
}
