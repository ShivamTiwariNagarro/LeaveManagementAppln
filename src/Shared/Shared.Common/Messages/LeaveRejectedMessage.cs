namespace Shared.Common.Messages;

/// <summary>
/// Message published when a leave request is rejected
/// </summary>
public class LeaveRejectedMessage
{
    public string LeaveId { get; set; } = string.Empty;
    public Guid LeaveRequestId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string ManagerId { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfDays { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime RejectedAt { get; set; }
    public string? CorrelationId { get; set; }
}
