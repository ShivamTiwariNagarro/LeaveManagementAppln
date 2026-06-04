using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs;
using EmployeeService.Services;

namespace EmployeeService.Controllers;

[ApiController]
[Route("api/employees/{employeeId}/balance")]
[Authorize]
public class LeaveBalanceController : ControllerBase
{
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<LeaveBalanceController> _logger;

    public LeaveBalanceController(
        ILeaveBalanceService leaveBalanceService,
        IEmployeeService employeeService,
        ILogger<LeaveBalanceController> logger)
    {
        _leaveBalanceService = leaveBalanceService;
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Get leave balance for an employee (optionally filter by leave type)
    /// </summary>
    [HttpGet]
    public IActionResult GetLeaveBalance(string employeeId, [FromQuery] string? leaveType = null, [FromQuery] int? year = null)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var currentUserId = User.FindFirst("user_id")?.Value;
        var currentUserRole = User.FindFirst("user_role")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting leave balance for employee: {EmployeeId}, leaveType: {LeaveType}, requested by: {RequesterId}",
            correlationId, employeeId, leaveType ?? "All", currentUserId);

        // Authorization: Users can only view their own balance, managers can view their team's balance
        if (currentUserRole != "Manager" && currentUserId != employeeId)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Unauthorized access attempt. User {UserId} tried to access balance of {EmployeeId}",
                correlationId, currentUserId, employeeId);
            return Forbid();
        }

        // If manager, verify they manage this employee
        if (currentUserRole == "Manager" && currentUserId != employeeId)
        {
            var employee = _employeeService.GetEmployeeById(employeeId);
            if (employee == null || employee.ManagerId != currentUserId)
            {
                _logger.LogWarning("CorrelationId: {CorrelationId} - Manager {ManagerId} does not manage employee {EmployeeId}",
                    correlationId, currentUserId, employeeId);
                return Forbid();
            }
        }

        var balance = _leaveBalanceService.GetLeaveBalance(employeeId, year);
        if (balance == null)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Leave balance not found for employee: {EmployeeId}",
                correlationId, employeeId);
            return NotFound(ApiResponse<object>.Fail("Employee or leave balance not found"));
        }

        // Filter by leave type if specified
        if (!string.IsNullOrEmpty(leaveType))
        {
            balance.Balances = balance.Balances
                .Where(b => b.LeaveType.Equals(leaveType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!balance.Balances.Any())
            {
                return NotFound(ApiResponse<object>.Fail($"No balance found for leave type: {leaveType}"));
            }
        }

        _logger.LogInformation("CorrelationId: {CorrelationId} - Successfully retrieved leave balance for employee: {EmployeeId}",
            correlationId, employeeId);
        return Ok(ApiResponse<object>.Ok(balance));
    }

    /// <summary>
    /// Get current user's leave balance (optionally filter by leave type)
    /// </summary>
    [HttpGet("/api/employees/me/balance")]
    public IActionResult GetMyLeaveBalance([FromQuery] string? leaveType = null, [FromQuery] int? year = null)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var currentUserId = User.FindFirst("user_id")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting leave balance for current user: {UserId}, leaveType: {LeaveType}",
            correlationId, currentUserId, leaveType ?? "All");

        if (string.IsNullOrEmpty(currentUserId))
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - User ID not found in JWT claims", correlationId);
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        var balance = _leaveBalanceService.GetLeaveBalance(currentUserId, year);
        if (balance == null)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Leave balance not found for user: {UserId}",
                correlationId, currentUserId);
            return NotFound(ApiResponse<object>.Fail("Leave balance not found"));
        }

        // Filter by leave type if specified
        if (!string.IsNullOrEmpty(leaveType))
        {
            balance.Balances = balance.Balances
                .Where(b => b.LeaveType.Equals(leaveType, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            if (!balance.Balances.Any())
            {
                return NotFound(ApiResponse<object>.Fail($"No balance found for leave type: {leaveType}"));
            }
        }

        return Ok(ApiResponse<object>.Ok(balance));
    }

    /// <summary>
    /// Deduct leave balance when leave is approved (Internal API - called by LeaveService)
    /// </summary>
    [HttpPut("{leaveType}/deduct")]
    public IActionResult DeductLeaveBalance(string employeeId, string leaveType, [FromQuery] int days, [FromQuery] int? year = null)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        
        _logger.LogInformation("CorrelationId: {CorrelationId} - Deducting {Days} days of {LeaveType} for employee {EmployeeId}",
            correlationId, days, leaveType, employeeId);

        var success = _leaveBalanceService.ApproveLeave(employeeId, leaveType, days, year);
        
        if (!success)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Failed to deduct balance for employee {EmployeeId}",
                correlationId, employeeId);
            return BadRequest(ApiResponse<object>.Fail("Failed to deduct leave balance. Insufficient pending days or employee not found."));
        }

        _logger.LogInformation("CorrelationId: {CorrelationId} - Successfully deducted {Days} days for employee {EmployeeId}",
            correlationId, days, employeeId);
        
        return Ok(ApiResponse<object>.Ok(new { deducted = days, leaveType, employeeId }, "Leave balance deducted successfully"));
    }

    /// <summary>
    /// Release pending leave balance when leave is rejected (Internal API - called by LeaveService)
    /// </summary>
    [HttpPut("{leaveType}/release")]
    public IActionResult ReleasePendingBalance(string employeeId, string leaveType, [FromQuery] int days, [FromQuery] int? year = null)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        
        _logger.LogInformation("CorrelationId: {CorrelationId} - Releasing {Days} pending days of {LeaveType} for employee {EmployeeId}",
            correlationId, days, leaveType, employeeId);

        var success = _leaveBalanceService.RejectLeave(employeeId, leaveType, days, year);
        
        if (!success)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Failed to release pending balance for employee {EmployeeId}",
                correlationId, employeeId);
            return BadRequest(ApiResponse<object>.Fail("Failed to release pending balance."));
        }

        return Ok(ApiResponse<object>.Ok(new { released = days, leaveType, employeeId }, "Pending balance released successfully"));
    }

    /// <summary>
    /// Reserve pending leave balance when leave is applied (Internal API - called by LeaveService)
    /// </summary>
    [HttpPut("{leaveType}/reserve")]
    public IActionResult ReservePendingBalance(string employeeId, string leaveType, [FromQuery] int days, [FromQuery] int? year = null)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        
        _logger.LogInformation("CorrelationId: {CorrelationId} - Reserving {Days} pending days of {LeaveType} for employee {EmployeeId}",
            correlationId, days, leaveType, employeeId);

        // Check if sufficient balance exists
        if (!_leaveBalanceService.HasSufficientBalance(employeeId, leaveType, days, year))
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Insufficient balance for employee {EmployeeId}",
                correlationId, employeeId);
            return BadRequest(ApiResponse<object>.Fail("Insufficient leave balance"));
        }

        var success = _leaveBalanceService.ReservePendingLeave(employeeId, leaveType, days, year);
        
        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail("Failed to reserve pending balance."));
        }

        return Ok(ApiResponse<object>.Ok(new { reserved = days, leaveType, employeeId }, "Leave balance reserved successfully"));
    }
}
