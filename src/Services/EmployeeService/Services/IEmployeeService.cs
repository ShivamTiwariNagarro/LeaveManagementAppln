using EmployeeService.Models;

namespace EmployeeService.Services;

public interface IEmployeeService
{
    EmployeeResponse? GetEmployeeById(string employeeId);
    EmployeeResponse? GetEmployeeByUsername(string username);
    IEnumerable<EmployeeResponse> GetAllEmployees();
    IEnumerable<EmployeeResponse> GetEmployeesByManager(string managerId);
}
