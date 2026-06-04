using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthService.Models;
using AuthService.Services;
using AuthService.Validators;
using Shared.Common.DTOs;
using Shared.Common.Extensions;
using Shared.Common.Middleware;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and generate JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user info on success</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var correlationId = HttpContext.GetCorrelationId();
        _logger.LogInformation("Login request received for username: {Username}. CorrelationId: {CorrelationId}", 
            request?.Username, correlationId);

        // Validate request using validator
        var (isValid, errorMessage) = LoginRequestValidator.Validate(request!);
        if (!isValid)
        {
            _logger.LogWarning("Login validation failed: {Error}", errorMessage);
            return BadRequest(ErrorResponse.Create(errorMessage!, ErrorCodes.VALIDATION_ERROR, correlationId));
        }

        // Authenticate
        var result = _authService.Authenticate(request!.Username, request.Password);

        if (result == null)
        {
            _logger.LogWarning("Login failed for username: {Username}", request.Username);
            return Unauthorized(ErrorResponse.Create("Invalid credentials", ErrorCodes.AUTH_INVALID_CREDENTIALS, correlationId));
        }

        _logger.LogInformation("Login successful for user: {UserId}", result.UserId);
        
        var response = ApiResponse<LoginResponse>.SuccessResponse(result, "Login successful");
        response.TraceId = correlationId;
        return Ok(response);
    }

    /// <summary>
    /// Validate JWT token and return user info
    /// </summary>
    /// <returns>User info if token is valid</returns>
    [HttpGet("validate")]
    [Authorize]
    public ActionResult<ApiResponse<object>> Validate()
    {
        var correlationId = HttpContext.GetCorrelationId();
        var userId = User.GetUserId();
        var username = User.GetUsername();
        var role = User.GetRole();

        _logger.LogInformation("Token validation request for user: {UserId}. CorrelationId: {CorrelationId}", 
            userId, correlationId);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ErrorResponse.Create("Token expired or invalid", ErrorCodes.AUTH_TOKEN_INVALID, correlationId));
        }

        var user = _authService.ValidateUser(userId);
        if (user == null)
        {
            return Unauthorized(ErrorResponse.Create("User not found", ErrorCodes.AUTH_TOKEN_INVALID, correlationId));
        }

        var response = new
        {
            UserId = userId,
            Username = username,
            Role = role,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Approximate
        };

        var apiResponse = ApiResponse<object>.SuccessResponse(response, "Token is valid");
        apiResponse.TraceId = correlationId;
        return Ok(apiResponse);
    }
}
