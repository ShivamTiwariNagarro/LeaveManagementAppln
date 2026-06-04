using LeaveService.Data;
using LeaveService.Models;
using Shared.Common.Constants;
using Shared.Common.Messaging;
using Shared.Common.Messages;
using Microsoft.AspNetCore.Http;

namespace LeaveService.Services;

public class LeaveRequestServiceImpl : ILeaveRequestService
{
    private readonly LeaveRequestDataStore _dataStore;
    private readonly RabbitMQPublisher _rabbitMQPublisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LeaveRequestServiceImpl> _logger;

    public LeaveRequestServiceImpl(
        LeaveRequestDataStore dataStore,
        RabbitMQPublisher rabbitMQPublisher,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LeaveRequestServiceImpl> logger)
    {
        _dataStore = dataStore;
        _rabbitMQPublisher = rabbitMQPublisher;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<LeaveRequestResponse> ApplyLeaveAsync(
        string employeeId, 
        string employeeName, 
        string? managerId, 
        ApplyLeaveRequest request)
    {
        _logger.LogInformation("Processing leave application for employee {EmployeeId}", employeeId);

        // Validate leave type
        if (!LeaveTypes.IsValid(request.LeaveType))
        {
            throw new ArgumentException($"Invalid leave type: {request.LeaveType}. Valid types are: {string.Join(", ", LeaveTypes.GetAllNames())}");
        }

        // Validate dates
        if (request.StartDate > request.EndDate)
        {
            throw new ArgumentException("Start date cannot be after end date");
        }

        if (request.StartDate.Date < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Cannot apply leave for past dates");
        }

        // Check for overlapping leave requests (Pending or Approved)
        var existingLeaves = _dataStore.GetByEmployeeId(employeeId)
            .Where(l => l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved);

        foreach (var existing in existingLeaves)
        {
            if (request.StartDate.Date <= existing.EndDate.Date && request.EndDate.Date >= existing.StartDate.Date)
            {
                throw new ArgumentException(
                    $"Leave request overlaps with an existing {existing.Status.ToLower()} leave ({existing.LeaveId}) " +
                    $"from {existing.StartDate:yyyy-MM-dd} to {existing.EndDate:yyyy-MM-dd}");
            }
        }

        // Calculate number of days (excluding weekends)
        var numberOfDays = CalculateWorkingDays(request.StartDate, request.EndDate);
        
        if (numberOfDays <= 0)
        {
            throw new ArgumentException("Leave period must include at least one working day");
        }

        // Create leave request
        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            NumberOfDays = numberOfDays,
            Reason = request.Reason,
            Status = LeaveStatus.Pending,
            ManagerId = managerId
        };

        var created = _dataStore.Create(leaveRequest);
        _logger.LogInformation("Leave request {LeaveId} created for employee {EmployeeId}", 
            created.LeaveId, employeeId);

        // Reserve pending balance in EmployeeService
        try
        {
            await ReservePendingBalanceAsync(employeeId, request.LeaveType, numberOfDays);
            _logger.LogInformation("Reserved {Days} pending days for employee {EmployeeId}", numberOfDays, employeeId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reserve pending balance for {LeaveId}. Balance may be inconsistent.", created.LeaveId);
        }

        // Publish message to RabbitMQ
        try
        {
            var message = new LeaveAppliedMessage
            {
                LeaveId = created.LeaveId,
                EmployeeId = employeeId,
                EmployeeName = employeeName,
                LeaveType = request.LeaveType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                NumberOfDays = numberOfDays,
                Reason = request.Reason,
                ManagerId = managerId,
                AppliedAt = created.CreatedAt
            };

            await Task.Run(() => _rabbitMQPublisher.Publish(message, "leave.applied"));
            _logger.LogInformation("Published LeaveAppliedMessage for {LeaveId}", created.LeaveId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish LeaveAppliedMessage for {LeaveId}. " +
                "Notification will not be sent, but leave request was created.", created.LeaveId);
        }

        return MapToResponse(created);
    }

    public LeaveRequestResponse? GetLeaveById(string leaveId)
    {
        var request = _dataStore.GetById(leaveId);
        return request == null ? null : MapToResponse(request);
    }

    public IEnumerable<LeaveRequestResponse> GetLeavesByEmployee(string employeeId, string? status = null)
    {
        return _dataStore.GetByEmployeeId(employeeId, status).Select(MapToResponse);
    }

    public IEnumerable<LeaveRequestResponse> GetPendingLeavesByManager(string managerId)
    {
        return _dataStore.GetPendingByManagerId(managerId).Select(MapToResponse);
    }

    public IEnumerable<LeaveRequestResponse> GetAllLeavesByManager(string managerId)
    {
        return _dataStore.GetByManagerId(managerId).Select(MapToResponse);
    }

    public async Task<LeaveRequestResponse?> ApproveLeaveAsync(string leaveId, string managerId, string? comments)
    {
        var request = _dataStore.GetById(leaveId);
        if (request == null)
        {
            _logger.LogWarning("Leave request {LeaveId} not found", leaveId);
            return null;
        }

        if (request.ManagerId != managerId)
        {
            _logger.LogWarning("Manager {ManagerId} is not authorized to approve leave {LeaveId}", 
                managerId, leaveId);
            throw new UnauthorizedAccessException("You are not authorized to approve this leave request");
        }

        if (request.Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve leave with status: {request.Status}");
        }

        // Deduct leave balance from Employee Service
        try
        {
            var client = CreateAuthenticatedClient("EmployeeService");
            var deductUrl = $"/api/employees/{request.EmployeeId}/balance/{request.LeaveType}/deduct?days={request.NumberOfDays}";
            
            _logger.LogInformation("Calling EmployeeService to deduct balance: {Url}", deductUrl);
            
            var response = await client.PutAsync(deductUrl, null);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to deduct leave balance: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                throw new InvalidOperationException("Failed to deduct leave balance from Employee Service");
            }
            
            _logger.LogInformation("Successfully deducted {Days} days of {LeaveType} for employee {EmployeeId}",
                request.NumberOfDays, request.LeaveType, request.EmployeeId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling EmployeeService to deduct balance");
            throw new InvalidOperationException("Unable to connect to Employee Service to deduct balance");
        }

        request.Status = LeaveStatus.Approved;
        request.ManagerComments = comments;
        request.ApprovedAt = DateTime.UtcNow;
        
        _dataStore.Update(request);
        _logger.LogInformation("Leave request {LeaveId} approved by manager {ManagerId}", leaveId, managerId);

        // Publish approval message
        try
        {
            var message = new LeaveApprovedMessage
            {
                LeaveId = leaveId,
                EmployeeId = request.EmployeeId,
                EmployeeName = request.EmployeeName,
                LeaveType = request.LeaveType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                NumberOfDays = request.NumberOfDays,
                ManagerId = managerId,
                ManagerComments = comments,
                ApprovedAt = request.ApprovedAt.Value
            };

            await Task.Run(() => _rabbitMQPublisher.Publish(message, "leave.approved"));
            _logger.LogInformation("Published LeaveApprovedMessage for {LeaveId}", leaveId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish LeaveApprovedMessage for {LeaveId}", leaveId);
        }

        return MapToResponse(request);
    }

    public async Task<LeaveRequestResponse?> RejectLeaveAsync(string leaveId, string managerId, string? comments)
    {
        var request = _dataStore.GetById(leaveId);
        if (request == null)
        {
            _logger.LogWarning("Leave request {LeaveId} not found", leaveId);
            return null;
        }

        if (request.ManagerId != managerId)
        {
            _logger.LogWarning("Manager {ManagerId} is not authorized to reject leave {LeaveId}", 
                managerId, leaveId);
            throw new UnauthorizedAccessException("You are not authorized to reject this leave request");
        }

        if (request.Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject leave with status: {request.Status}");
        }

        // Release pending balance back to available
        try
        {
            await ReleasePendingBalanceAsync(request.EmployeeId, request.LeaveType, request.NumberOfDays);
            _logger.LogInformation("Released {Days} pending days for employee {EmployeeId}", 
                request.NumberOfDays, request.EmployeeId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release pending balance for {LeaveId}", leaveId);
        }

        request.Status = LeaveStatus.Rejected;
        request.ManagerComments = comments;
        request.RejectedAt = DateTime.UtcNow;
        
        _dataStore.Update(request);
        _logger.LogInformation("Leave request {LeaveId} rejected by manager {ManagerId}", leaveId, managerId);

        // Publish rejection message
        try
        {
            var message = new LeaveRejectedMessage
            {
                LeaveId = leaveId,
                EmployeeId = request.EmployeeId,
                EmployeeName = request.EmployeeName,
                LeaveType = request.LeaveType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                NumberOfDays = request.NumberOfDays,
                ManagerId = managerId,
                RejectionReason = comments ?? "No reason provided",
                RejectedAt = request.RejectedAt.Value
            };

            await Task.Run(() => _rabbitMQPublisher.Publish(message, "leave.rejected"));
            _logger.LogInformation("Published LeaveRejectedMessage for {LeaveId}", leaveId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish LeaveRejectedMessage for {LeaveId}", leaveId);
        }

        return MapToResponse(request);
    }

    public async Task<LeaveRequestResponse?> CancelLeaveAsync(string leaveId, string employeeId)
    {
        var request = _dataStore.GetById(leaveId);
        if (request == null)
        {
            return null;
        }

        if (request.EmployeeId != employeeId)
        {
            throw new UnauthorizedAccessException("You can only cancel your own leave requests");
        }

        if (request.Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot cancel leave with status: {request.Status}");
        }

        // Release pending balance back to available
        try
        {
            await ReleasePendingBalanceAsync(request.EmployeeId, request.LeaveType, request.NumberOfDays);
            _logger.LogInformation("Released {Days} pending days for cancelled leave {LeaveId}", 
                request.NumberOfDays, leaveId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release pending balance for cancelled leave {LeaveId}. Balance may be inconsistent.", leaveId);
        }

        request.Status = LeaveStatus.Cancelled;
        _dataStore.Update(request);
        
        _logger.LogInformation("Leave request {LeaveId} cancelled by employee {EmployeeId}", leaveId, employeeId);

        // Publish cancellation message
        try
        {
            var message = new LeaveCancelledMessage
            {
                LeaveId = leaveId,
                EmployeeId = request.EmployeeId,
                EmployeeName = request.EmployeeName,
                ManagerId = request.ManagerId,
                LeaveType = request.LeaveType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                NumberOfDays = request.NumberOfDays,
                CancelledAt = DateTime.UtcNow
            };
            await Task.Run(() => _rabbitMQPublisher.Publish(message, "leave.cancelled"));
            _logger.LogInformation("Published LeaveCancelledMessage for {LeaveId}", leaveId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish LeaveCancelledMessage for {LeaveId}", leaveId);
        }

        return MapToResponse(request);
    }

    private static int CalculateWorkingDays(DateTime start, DateTime end)
    {
        int days = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                days++;
            }
        }
        return days;
    }

    private static LeaveRequestResponse MapToResponse(LeaveRequest request)
    {
        return new LeaveRequestResponse
        {
            LeaveId = request.LeaveId,
            EmployeeId = request.EmployeeId,
            EmployeeName = request.EmployeeName,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            NumberOfDays = request.NumberOfDays,
            Reason = request.Reason,
            Status = request.Status,
            ManagerComments = request.ManagerComments,
            CreatedAt = request.CreatedAt,
            ApprovedAt = request.ApprovedAt,
            RejectedAt = request.RejectedAt
        };
    }

    /// <summary>
    /// Creates an HTTP client with the authorization header forwarded from the current request.
    /// This ensures service-to-service calls are authenticated.
    /// </summary>
    private HttpClient CreateAuthenticatedClient(string clientName)
    {
        var client = _httpClientFactory.CreateClient(clientName);
        
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader))
        {
            client.DefaultRequestHeaders.Add("Authorization", authHeader);
        }
        
        return client;
    }

    private async Task ReservePendingBalanceAsync(string employeeId, string leaveType, int days)
    {
        var client = CreateAuthenticatedClient("EmployeeService");
        var reserveUrl = $"/api/employees/{employeeId}/balance/{leaveType}/reserve?days={days}";
        
        _logger.LogInformation("Calling EmployeeService to reserve pending balance: {Url}", reserveUrl);
        
        var response = await client.PutAsync(reserveUrl, null);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to reserve pending balance: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);
            throw new InvalidOperationException($"Failed to reserve pending balance: {errorContent}");
        }
    }

    private async Task ReleasePendingBalanceAsync(string employeeId, string leaveType, int days)
    {
        var client = CreateAuthenticatedClient("EmployeeService");
        var releaseUrl = $"/api/employees/{employeeId}/balance/{leaveType}/release?days={days}";
        
        _logger.LogInformation("Calling EmployeeService to release pending balance: {Url}", releaseUrl);
        
        var response = await client.PutAsync(releaseUrl, null);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to release pending balance: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);
        }
    }
}
