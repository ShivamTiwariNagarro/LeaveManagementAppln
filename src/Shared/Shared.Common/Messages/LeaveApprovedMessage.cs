namespace Shared.Common.Messages;

/// <summary>
/// Message published when a leave request is approved
/// </summary>
public class LeaveApprovedMessage
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
    public int RemainingBalance { get; set; }
    public DateTime ApprovedAt { get; set; }
    public string? Comments { get; set; }
    public string? ManagerComments { get; set; }
    public string? CorrelationId { get; set; }
}
