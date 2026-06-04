using LeaveService.Models;

namespace LeaveService.Services;

public interface ILeaveRequestService
{
    Task<LeaveRequestResponse> ApplyLeaveAsync(string employeeId, string employeeName, string? managerId, ApplyLeaveRequest request);
    LeaveRequestResponse? GetLeaveById(string leaveId);
    IEnumerable<LeaveRequestResponse> GetLeavesByEmployee(string employeeId, string? status = null);
    IEnumerable<LeaveRequestResponse> GetPendingLeavesByManager(string managerId);
    IEnumerable<LeaveRequestResponse> GetAllLeavesByManager(string managerId);
    Task<LeaveRequestResponse?> ApproveLeaveAsync(string leaveId, string managerId, string? comments);
    Task<LeaveRequestResponse?> RejectLeaveAsync(string leaveId, string managerId, string? comments);
    Task<LeaveRequestResponse?> CancelLeaveAsync(string leaveId, string employeeId);
}
