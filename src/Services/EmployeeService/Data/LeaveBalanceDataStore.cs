using System.Collections.Concurrent;
using EmployeeService.Models;
using Shared.Common.Constants;

namespace EmployeeService.Data;

/// <summary>
/// In-memory leave balance data store with auto-allocation for all employees
/// </summary>
public class LeaveBalanceDataStore
{
    private readonly ConcurrentDictionary<string, LeaveBalance> _balances = new();
    private readonly EmployeeDataStore _employeeDataStore;

    // Default leave allocation per year
    private static readonly Dictionary<string, int> DefaultAllocation = new()
    {
        { LeaveTypes.Casual, 12 },
        { LeaveTypes.Sick, 10 },
        { LeaveTypes.Privilege, 15 }
    };

    public LeaveBalanceDataStore(EmployeeDataStore employeeDataStore)
    {
        _employeeDataStore = employeeDataStore;
        InitializeBalances();
    }

    private void InitializeBalances()
    {
        var currentYear = DateTime.UtcNow.Year;
        var employees = _employeeDataStore.GetAll();

        foreach (var employee in employees)
        {
            foreach (var allocation in DefaultAllocation)
            {
                var balance = new LeaveBalance
                {
                    EmployeeId = employee.EmployeeId,
                    LeaveType = allocation.Key,
                    TotalDays = allocation.Value,
                    UsedDays = 0,
                    PendingDays = 0,
                    Year = currentYear
                };

                var key = GetKey(employee.EmployeeId, allocation.Key, currentYear);
                _balances.TryAdd(key, balance);
            }
        }
    }

    private static string GetKey(string employeeId, string leaveType, int year)
    {
        return $"{employeeId}_{leaveType}_{year}";
    }

    public IEnumerable<LeaveBalance> GetByEmployeeId(string employeeId, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        return _balances.Values
            .Where(b => b.EmployeeId == employeeId && b.Year == targetYear)
            .ToList();
    }

    public LeaveBalance? GetByEmployeeAndType(string employeeId, string leaveType, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var key = GetKey(employeeId, leaveType, targetYear);
        _balances.TryGetValue(key, out var balance);
        return balance;
    }

    public bool UpdateBalance(string employeeId, string leaveType, int usedDays, int pendingDays, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var key = GetKey(employeeId, leaveType, targetYear);

        if (_balances.TryGetValue(key, out var balance))
        {
            balance.UsedDays = usedDays;
            balance.PendingDays = pendingDays;
            return true;
        }
        return false;
    }

    public bool AddPendingDays(string employeeId, string leaveType, int days, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var key = GetKey(employeeId, leaveType, targetYear);

        if (_balances.TryGetValue(key, out var balance))
        {
            if (balance.AvailableDays >= days)
            {
                balance.PendingDays += days;
                return true;
            }
        }
        return false;
    }

    public bool ConvertPendingToUsed(string employeeId, string leaveType, int days, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var key = GetKey(employeeId, leaveType, targetYear);

        if (_balances.TryGetValue(key, out var balance))
        {
            if (balance.PendingDays >= days)
            {
                balance.PendingDays -= days;
                balance.UsedDays += days;
                return true;
            }
        }
        return false;
    }

    public bool ReleasePendingDays(string employeeId, string leaveType, int days, int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var key = GetKey(employeeId, leaveType, targetYear);

        if (_balances.TryGetValue(key, out var balance))
        {
            if (balance.PendingDays >= days)
            {
                balance.PendingDays -= days;
                return true;
            }
        }
        return false;
    }
}
