using EmployeeService.Models;

namespace EmployeeService.Services;

public interface ILeaveBalanceService
{
    LeaveBalanceResponse? GetLeaveBalance(string employeeId, int? year = null);
    bool HasSufficientBalance(string employeeId, string leaveType, int days, int? year = null);
    bool ReservePendingLeave(string employeeId, string leaveType, int days, int? year = null);
    bool ApproveLeave(string employeeId, string leaveType, int days, int? year = null);
    bool RejectLeave(string employeeId, string leaveType, int days, int? year = null);
}
