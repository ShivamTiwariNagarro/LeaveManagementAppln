using System.Collections.Concurrent;
using LeaveService.Models;

namespace LeaveService.Data;

/// <summary>
/// In-memory leave request data store
/// </summary>
public class LeaveRequestDataStore
{
    private readonly ConcurrentDictionary<string, LeaveRequest> _leaveRequests = new();
    private int _counter = 0;

    public LeaveRequestDataStore()
    {
    }

    private string GenerateLeaveId()
    {
        return $"LR-{DateTime.UtcNow:yyyyMMdd}-{Interlocked.Increment(ref _counter):D4}";
    }

    public LeaveRequest Create(LeaveRequest request)
    {
        request.LeaveId = GenerateLeaveId();
        request.CreatedAt = DateTime.UtcNow;
        _leaveRequests.TryAdd(request.LeaveId, request);
        return request;
    }

    public LeaveRequest? GetById(string leaveId)
    {
        _leaveRequests.TryGetValue(leaveId, out var request);
        return request;
    }

    public IEnumerable<LeaveRequest> GetByEmployeeId(string employeeId, string? status = null)
    {
        var query = _leaveRequests.Values.Where(r => r.EmployeeId == employeeId);
        
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }
        
        return query.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public IEnumerable<LeaveRequest> GetByManagerId(string managerId, string? status = null)
    {
        var query = _leaveRequests.Values.Where(r => r.ManagerId == managerId);
        
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }
        
        return query.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public IEnumerable<LeaveRequest> GetPendingByManagerId(string managerId)
    {
        return GetByManagerId(managerId, LeaveStatus.Pending);
    }

    public bool Update(LeaveRequest request)
    {
        if (_leaveRequests.ContainsKey(request.LeaveId))
        {
            request.UpdatedAt = DateTime.UtcNow;
            _leaveRequests[request.LeaveId] = request;
            return true;
        }
        return false;
    }

    public bool Delete(string leaveId)
    {
        return _leaveRequests.TryRemove(leaveId, out _);
    }

    public IEnumerable<LeaveRequest> GetAll()
    {
        return _leaveRequests.Values.OrderByDescending(r => r.CreatedAt).ToList();
    }
}
