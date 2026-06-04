using EmployeeService.Data;
using EmployeeService.Models;

namespace EmployeeService.Services;

public class LeaveBalanceServiceImpl : ILeaveBalanceService
{
    private readonly LeaveBalanceDataStore _leaveBalanceDataStore;
    private readonly EmployeeDataStore _employeeDataStore;
    private readonly ILogger<LeaveBalanceServiceImpl> _logger;

    public LeaveBalanceServiceImpl(
        LeaveBalanceDataStore leaveBalanceDataStore,
        EmployeeDataStore employeeDataStore,
        ILogger<LeaveBalanceServiceImpl> logger)
    {
        _leaveBalanceDataStore = leaveBalanceDataStore;
        _employeeDataStore = employeeDataStore;
        _logger = logger;
    }

    public LeaveBalanceResponse? GetLeaveBalance(string employeeId, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        _logger.LogDebug("Getting leave balance for employee {EmployeeId}, year {Year}", employeeId, targetYear);

        var employee = _employeeDataStore.GetById(employeeId);
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {EmployeeId}", employeeId);
            return null;
        }

        var balances = _leaveBalanceDataStore.GetByEmployeeId(employeeId, targetYear);
        
        return new LeaveBalanceResponse
        {
            EmployeeId = employeeId,
            EmployeeName = employee.FullName,
            Year = targetYear,
            Balances = balances.Select(b => new LeaveTypeBalance
            {
                LeaveType = b.LeaveType,
                TotalDays = b.TotalDays,
                UsedDays = b.UsedDays,
                PendingDays = b.PendingDays,
                AvailableDays = b.AvailableDays
            }).ToList()
        };
    }

    public bool HasSufficientBalance(string employeeId, string leaveType, int days, int? year = null)
    {
        var balance = _leaveBalanceDataStore.GetByEmployeeAndType(employeeId, leaveType, year);
        if (balance == null)
        {
            _logger.LogWarning("Leave balance not found for employee {EmployeeId}, type {LeaveType}", 
                employeeId, leaveType);
            return false;
        }

        var hasSufficient = balance.AvailableDays >= days;
        _logger.LogDebug("Balance check for {EmployeeId}: Available={Available}, Requested={Requested}, Sufficient={Sufficient}",
            employeeId, balance.AvailableDays, days, hasSufficient);
        
        return hasSufficient;
    }

    public bool ReservePendingLeave(string employeeId, string leaveType, int days, int? year = null)
    {
        _logger.LogInformation("Reserving {Days} days of {LeaveType} leave for employee {EmployeeId}",
            days, leaveType, employeeId);
        
        return _leaveBalanceDataStore.AddPendingDays(employeeId, leaveType, days, year);
    }

    public bool ApproveLeave(string employeeId, string leaveType, int days, int? year = null)
    {
        _logger.LogInformation("Converting {Days} days from pending to used for employee {EmployeeId}, type {LeaveType}",
            days, employeeId, leaveType);
        
        return _leaveBalanceDataStore.ConvertPendingToUsed(employeeId, leaveType, days, year);
    }

    public bool RejectLeave(string employeeId, string leaveType, int days, int? year = null)
    {
        _logger.LogInformation("Releasing {Days} pending days for employee {EmployeeId}, type {LeaveType}",
            days, employeeId, leaveType);
        
        return _leaveBalanceDataStore.ReleasePendingDays(employeeId, leaveType, days, year);
    }
}
