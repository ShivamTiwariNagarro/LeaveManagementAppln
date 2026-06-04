using System.Collections.Concurrent;
using EmployeeService.Models;
using Shared.Common.Constants;

namespace EmployeeService.Data;

/// <summary>
/// In-memory employee data store with pre-seeded data matching AuthService users
/// </summary>
public class EmployeeDataStore
{
    private readonly ConcurrentDictionary<string, Employee> _employees = new();

    public EmployeeDataStore()
    {
        SeedData();
    }

    private void SeedData()
    {
        // Pre-seed employees matching AuthService users
        var employees = new List<Employee>
        {
            // Managers
            new Employee
            {
                EmployeeId = "mgr-001",
                Username = "manager1",
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@company.com",
                Department = "Engineering",
                Role = Roles.Manager,
                ManagerId = null,
                JoiningDate = new DateTime(2020, 1, 15),
                IsActive = true
            },
            new Employee
            {
                EmployeeId = "mgr-002",
                Username = "manager2",
                FirstName = "Sarah",
                LastName = "Johnson",
                Email = "sarah.johnson@company.com",
                Department = "Finance",
                Role = Roles.Manager,
                ManagerId = null,
                JoiningDate = new DateTime(2019, 6, 1),
                IsActive = true
            },
            // Employees under manager1
            new Employee
            {
                EmployeeId = "emp-001",
                Username = "employee1",
                FirstName = "Michael",
                LastName = "Brown",
                Email = "michael.brown@company.com",
                Department = "Engineering",
                Role = Roles.Employee,
                ManagerId = "mgr-001",
                JoiningDate = new DateTime(2022, 3, 10),
                IsActive = true
            },
            new Employee
            {
                EmployeeId = "emp-002",
                Username = "employee2",
                FirstName = "Emily",
                LastName = "Davis",
                Email = "emily.davis@company.com",
                Department = "Engineering",
                Role = Roles.Employee,
                ManagerId = "mgr-001",
                JoiningDate = new DateTime(2023, 1, 5),
                IsActive = true
            },
            // Employees under manager2
            new Employee
            {
                EmployeeId = "emp-003",
                Username = "employee3",
                FirstName = "David",
                LastName = "Wilson",
                Email = "david.wilson@company.com",
                Department = "Finance",
                Role = Roles.Employee,
                ManagerId = "mgr-002",
                JoiningDate = new DateTime(2021, 8, 20),
                IsActive = true
            },
            new Employee
            {
                EmployeeId = "emp-004",
                Username = "employee4",
                FirstName = "Lisa",
                LastName = "Taylor",
                Email = "lisa.taylor@company.com",
                Department = "Finance",
                Role = Roles.Employee,
                ManagerId = "mgr-002",
                JoiningDate = new DateTime(2024, 2, 1),
                IsActive = true
            }
        };

        foreach (var employee in employees)
        {
            _employees.TryAdd(employee.EmployeeId, employee);
        }
    }

    public Employee? GetById(string employeeId)
    {
        _employees.TryGetValue(employeeId, out var employee);
        return employee;
    }

    public Employee? GetByUsername(string username)
    {
        return _employees.Values.FirstOrDefault(e => 
            e.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Employee> GetAll()
    {
        return _employees.Values.ToList();
    }

    public IEnumerable<Employee> GetByManagerId(string managerId)
    {
        return _employees.Values.Where(e => e.ManagerId == managerId).ToList();
    }
}
