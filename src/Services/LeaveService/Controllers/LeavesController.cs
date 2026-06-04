using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs;
using LeaveService.Models;
using LeaveService.Services;

namespace LeaveService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeavesController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly ILogger<LeavesController> _logger;

    public LeavesController(ILeaveRequestService leaveRequestService, ILogger<LeavesController> logger)
    {
        _leaveRequestService = leaveRequestService;
        _logger = logger;
    }

    /// <summary>
    /// Apply for leave
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ApplyLeave([FromBody] ApplyLeaveRequest request)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var employeeId = User.FindFirst("user_id")?.Value;
        var employeeName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value 
                           ?? User.FindFirst("name")?.Value 
                           ?? "Unknown";
        var managerId = User.FindFirst("manager_id")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Employee {EmployeeId} applying for {LeaveType} leave",
            correlationId, employeeId, request.LeaveType);

        if (string.IsNullOrEmpty(employeeId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        try
        {
            var result = await _leaveRequestService.ApplyLeaveAsync(employeeId, employeeName, managerId, request);
            
            _logger.LogInformation("CorrelationId: {CorrelationId} - Leave request {LeaveId} created successfully",
                correlationId, result.LeaveId);
            
            return CreatedAtAction(nameof(GetLeaveById), new { leaveId = result.LeaveId }, 
                ApiResponse<LeaveRequestResponse>.Ok(result, "Leave application submitted successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Validation error: {Error}", correlationId, ex.Message);
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get leave request by ID
    /// </summary>
    [HttpGet("{leaveId}")]
    public IActionResult GetLeaveById(string leaveId)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var userId = User.FindFirst("user_id")?.Value;
        var userRole = User.FindFirst("user_role")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting leave request {LeaveId}", 
            correlationId, leaveId);

        var leave = _leaveRequestService.GetLeaveById(leaveId);
        if (leave == null)
        {
            return NotFound(ApiResponse<object>.Fail("Leave request not found"));
        }

        // Authorization: Employee can only view their own, Manager can view their team's
        if (userRole != "Manager" && leave.EmployeeId != userId)
        {
            return Forbid();
        }

        return Ok(ApiResponse<LeaveRequestResponse>.Ok(leave));
    }

    /// <summary>
    /// Get current user's leave history with pagination and filtering
    /// Supports: status filter (All/Approved/Rejected/Pending/Cancelled), pagination
    /// </summary>
    [HttpGet("my")]
    public IActionResult GetMyLeaves(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var employeeId = User.FindFirst("user_id")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting leaves for employee {EmployeeId}, status: {Status}, page: {Page}, pageSize: {PageSize}",
            correlationId, employeeId, status ?? "All", page, pageSize);

        if (string.IsNullOrEmpty(employeeId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        // Ensure valid pagination values
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var allLeaves = _leaveRequestService.GetLeavesByEmployee(employeeId, status);
        var totalCount = allLeaves.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var pagedLeaves = allLeaves.Skip((page - 1) * pageSize).Take(pageSize);

        var result = new
        {
            leaves = pagedLeaves,
            pagination = new
            {
                currentPage = page,
                pageSize,
                totalCount,
                totalPages
            }
        };

        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>
    /// Get pending leave requests for manager's team
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Manager")]
    public IActionResult GetPendingLeaves()
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var managerId = User.FindFirst("user_id")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting pending leaves for manager {ManagerId}",
            correlationId, managerId);

        if (string.IsNullOrEmpty(managerId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        var leaves = _leaveRequestService.GetPendingLeavesByManager(managerId);
        return Ok(ApiResponse<IEnumerable<LeaveRequestResponse>>.Ok(leaves));
    }

    /// <summary>
    /// Get all leave requests for manager's team
    /// Filter by: Status (Pending/Approved/Rejected), Employee, Date Range
    /// </summary>
    [HttpGet("team")]
    [Authorize(Roles = "Manager")]
    public IActionResult GetTeamLeaves(
        [FromQuery] string? status = null,
        [FromQuery] string? employeeId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var managerId = User.FindFirst("user_id")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Getting team leaves for manager {ManagerId}. Filters - status: {Status}, employee: {Employee}, dateRange: {Start} to {End}",
            correlationId, managerId, status ?? "All", employeeId ?? "All", startDate, endDate);

        if (string.IsNullOrEmpty(managerId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        var leaves = _leaveRequestService.GetAllLeavesByManager(managerId);

        // Filter by status
        if (!string.IsNullOrEmpty(status))
        {
            leaves = leaves.Where(l => l.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by employee
        if (!string.IsNullOrEmpty(employeeId))
        {
            leaves = leaves.Where(l => l.EmployeeId == employeeId);
        }

        // Filter by date range
        if (startDate.HasValue)
        {
            leaves = leaves.Where(l => l.EndDate >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            leaves = leaves.Where(l => l.StartDate <= endDate.Value.Date);
        }

        return Ok(ApiResponse<IEnumerable<LeaveRequestResponse>>.Ok(leaves));
    }

    /// <summary>
    /// Approve a leave request
    /// </summary>
    [HttpPost("{leaveId}/approve")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ApproveLeave(string leaveId, [FromBody] ApproveRejectRequest? request)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var managerId = User.FindFirst("user_id")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Manager {ManagerId} approving leave {LeaveId}",
            correlationId, managerId, leaveId);

        if (string.IsNullOrEmpty(managerId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        try
        {
            var result = await _leaveRequestService.ApproveLeaveAsync(leaveId, managerId, request?.Comments);
            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail("Leave request not found"));
            }

            _logger.LogInformation("CorrelationId: {CorrelationId} - Leave {LeaveId} approved successfully",
                correlationId, leaveId);
            
            return Ok(ApiResponse<LeaveRequestResponse>.Ok(result, "Leave request approved successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Authorization failed: {Error}", correlationId, ex.Message);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Invalid operation: {Error}", correlationId, ex.Message);
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Reject a leave request
    /// </summary>
    [HttpPost("{leaveId}/reject")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> RejectLeave(string leaveId, [FromBody] ApproveRejectRequest? request)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var managerId = User.FindFirst("user_id")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Manager {ManagerId} rejecting leave {LeaveId}",
            correlationId, managerId, leaveId);

        if (string.IsNullOrEmpty(managerId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        try
        {
            var result = await _leaveRequestService.RejectLeaveAsync(leaveId, managerId, request?.Comments);
            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail("Leave request not found"));
            }

            _logger.LogInformation("CorrelationId: {CorrelationId} - Leave {LeaveId} rejected",
                correlationId, leaveId);
            
            return Ok(ApiResponse<LeaveRequestResponse>.Ok(result, "Leave request rejected"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Authorization failed: {Error}", correlationId, ex.Message);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Invalid operation: {Error}", correlationId, ex.Message);
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Cancel a pending leave request
    /// </summary>
    [HttpPost("{leaveId}/cancel")]
    public async Task<IActionResult> CancelLeave(string leaveId)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var employeeId = User.FindFirst("user_id")?.Value;

        _logger.LogInformation("CorrelationId: {CorrelationId} - Employee {EmployeeId} cancelling leave {LeaveId}",
            correlationId, employeeId, leaveId);

        if (string.IsNullOrEmpty(employeeId))
        {
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token"));
        }

        try
        {
            var result = await _leaveRequestService.CancelLeaveAsync(leaveId, employeeId);
            if (result == null)
            {
                return NotFound(ApiResponse<object>.Fail("Leave request not found"));
            }

            _logger.LogInformation("CorrelationId: {CorrelationId} - Leave {LeaveId} cancelled",
                correlationId, leaveId);
            
            return Ok(ApiResponse<LeaveRequestResponse>.Ok(result, "Leave request cancelled"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Authorization failed: {Error}", correlationId, ex.Message);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("CorrelationId: {CorrelationId} - Invalid operation: {Error}", correlationId, ex.Message);
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
