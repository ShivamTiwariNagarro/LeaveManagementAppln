using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs;
using EmployeeService.Services;

namespace EmployeeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Get employee by ID
    /// </summary>
    [HttpGet("{employeeId}")]
    public IActionResult GetById(string employeeId)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting employee details for: {EmployeeId}", 
            correlationId, employeeId);

        var employee = _employeeService.GetEmployeeById(employeeId);
        if (employee == null)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Employee not found: {EmployeeId}", 
                correlationId, employeeId);
            return NotFound(ApiResponse<object>.Fail("Employee not found"));
        }

        _logger.LogInformation("CorrelationId: {CorrelationId} - Successfully retrieved employee: {EmployeeId}", 
            correlationId, employeeId);
        return Ok(ApiResponse<object>.Ok(employee));
    }

    /// <summary>
    /// Get all employees (Manager only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Manager")]
    public IActionResult GetAll()
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting all employees", correlationId);

        var employees = _employeeService.GetAllEmployees();
        return Ok(ApiResponse<object>.Ok(employees));
    }

    /// <summary>
    /// Get employees under a specific manager
    /// </summary>
    [HttpGet("manager/{managerId}")]
    [Authorize(Roles = "Manager")]
    public IActionResult GetByManager(string managerId)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting employees for manager: {ManagerId}", 
            correlationId, managerId);

        var employees = _employeeService.GetEmployeesByManager(managerId);
        return Ok(ApiResponse<object>.Ok(employees));
    }

    /// <summary>
    /// Get current user's profile from JWT claims
    /// </summary>
    [HttpGet("me")]
    public IActionResult GetCurrentEmployee()
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var userId = User.FindFirst("user_id")?.Value;
        
        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting profile for current user: {UserId}", 
            correlationId, userId);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - User ID not found in JWT claims", correlationId);
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        var employee = _employeeService.GetEmployeeById(userId);
        if (employee == null)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Employee not found for user: {UserId}", 
                correlationId, userId);
            return NotFound(ApiResponse<object>.Fail("Employee not found"));
        }

        return Ok(ApiResponse<object>.Ok(employee));
    }
}
