using System.Collections.Concurrent;
using AuthService.Models;

namespace AuthService.Data;

/// <summary>
/// In-memory user data store with pre-seeded users
/// 
/// Pre-seeded users:
/// - 2 Managers: manager1, manager2
/// - 4 Employees: employee1, employee2 (report to manager1), employee3, employee4 (report to manager2)
/// 
/// All passwords are "password123" for testing
/// </summary>
public class UserDataStore
{
    private readonly ConcurrentDictionary<string, User> _users = new();
    private readonly ConcurrentDictionary<string, User> _usersByUsername = new();

    public UserDataStore()
    {
        SeedUsers();
    }

    private void SeedUsers()
    {
        // Manager 1
        var manager1 = new User
        {
            UserId = "mgr-001",
            Username = "manager1",
            Password = "password123",
            Email = "manager1@company.com",
            FullName = "Jane Manager",
            Role = "Manager",
            ManagerId = null,
            TeamMemberIds = new List<string> { "emp-001", "emp-002" },
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Manager 2
        var manager2 = new User
        {
            UserId = "mgr-002",
            Username = "manager2",
            Password = "password123",
            Email = "manager2@company.com",
            FullName = "John Director",
            Role = "Manager",
            ManagerId = null,
            TeamMemberIds = new List<string> { "emp-003", "emp-004" },
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Employee 1 (reports to Manager 1)
        var employee1 = new User
        {
            UserId = "emp-001",
            Username = "employee1",
            Password = "password123",
            Email = "employee1@company.com",
            FullName = "Alice Employee",
            Role = "Employee",
            ManagerId = "mgr-001",
            TeamMemberIds = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Employee 2 (reports to Manager 1)
        var employee2 = new User
        {
            UserId = "emp-002",
            Username = "employee2",
            Password = "password123",
            Email = "employee2@company.com",
            FullName = "Bob Employee",
            Role = "Employee",
            ManagerId = "mgr-001",
            TeamMemberIds = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Employee 3 (reports to Manager 2)
        var employee3 = new User
        {
            UserId = "emp-003",
            Username = "employee3",
            Password = "password123",
            Email = "employee3@company.com",
            FullName = "Charlie Employee",
            Role = "Employee",
            ManagerId = "mgr-002",
            TeamMemberIds = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Employee 4 (reports to Manager 2)
        var employee4 = new User
        {
            UserId = "emp-004",
            Username = "employee4",
            Password = "password123",
            Email = "employee4@company.com",
            FullName = "Diana Employee",
            Role = "Employee",
            ManagerId = "mgr-002",
            TeamMemberIds = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Add all users to dictionaries
        var users = new[] { manager1, manager2, employee1, employee2, employee3, employee4 };
        foreach (var user in users)
        {
            _users.TryAdd(user.UserId, user);
            _usersByUsername.TryAdd(user.Username.ToLower(), user);
        }
    }

    public User? GetById(string userId)
    {
        _users.TryGetValue(userId, out var user);
        return user;
    }

    public User? GetByUsername(string username)
    {
        _usersByUsername.TryGetValue(username.ToLower(), out var user);
        return user;
    }

    public IEnumerable<User> GetAll()
    {
        return _users.Values;
    }

    public IEnumerable<User> GetTeamMembers(string managerId)
    {
        var manager = GetById(managerId);
        if (manager == null || manager.Role != "Manager")
            return Enumerable.Empty<User>();

        return manager.TeamMemberIds
            .Select(id => GetById(id))
            .Where(u => u != null)
            .Cast<User>();
    }
}
