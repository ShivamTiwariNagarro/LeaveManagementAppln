using EmployeeService.Data;
using EmployeeService.Models;

namespace EmployeeService.Services;

public class EmployeeServiceImpl : IEmployeeService
{
    private readonly EmployeeDataStore _employeeDataStore;
    private readonly ILogger<EmployeeServiceImpl> _logger;

    public EmployeeServiceImpl(EmployeeDataStore employeeDataStore, ILogger<EmployeeServiceImpl> logger)
    {
        _employeeDataStore = employeeDataStore;
        _logger = logger;
    }

    public EmployeeResponse? GetEmployeeById(string employeeId)
    {
        _logger.LogDebug("Looking up employee by ID: {EmployeeId}", employeeId);
        var employee = _employeeDataStore.GetById(employeeId);
        return employee == null ? null : MapToResponse(employee);
    }

    public EmployeeResponse? GetEmployeeByUsername(string username)
    {
        _logger.LogDebug("Looking up employee by username: {Username}", username);
        var employee = _employeeDataStore.GetByUsername(username);
        return employee == null ? null : MapToResponse(employee);
    }

    public IEnumerable<EmployeeResponse> GetAllEmployees()
    {
        _logger.LogDebug("Retrieving all employees");
        return _employeeDataStore.GetAll().Select(MapToResponse);
    }

    public IEnumerable<EmployeeResponse> GetEmployeesByManager(string managerId)
    {
        _logger.LogDebug("Retrieving employees for manager: {ManagerId}", managerId);
        return _employeeDataStore.GetByManagerId(managerId).Select(MapToResponse);
    }

    private EmployeeResponse MapToResponse(Employee employee)
    {
        string? managerName = null;
        if (!string.IsNullOrEmpty(employee.ManagerId))
        {
            var manager = _employeeDataStore.GetById(employee.ManagerId);
            managerName = manager?.FullName;
        }

        return new EmployeeResponse
        {
            EmployeeId = employee.EmployeeId,
            Username = employee.Username,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Department = employee.Department,
            Role = employee.Role,
            ManagerId = employee.ManagerId,
            ManagerName = managerName,
            JoiningDate = employee.JoiningDate
        };
    }
}
