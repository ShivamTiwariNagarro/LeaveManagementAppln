namespace Shared.Common.DTOs;

/// <summary>
/// Standard error response format for all API errors
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    public static ErrorResponse Create(string error, string code, string? traceId = null)
    {
        return new ErrorResponse
        {
            Error = error,
            Code = code,
            TraceId = traceId
        };
    }

    public static ErrorResponse ValidationError(Dictionary<string, string[]> errors, string? traceId = null)
    {
        return new ErrorResponse
        {
            Error = "One or more validation errors occurred",
            Code = "VALIDATION_ERROR",
            TraceId = traceId,
            ValidationErrors = errors
        };
    }
}

/// <summary>
/// Common error codes used across all services
/// </summary>
public static class ErrorCodes
{
    // Authentication errors
    public const string AUTH_INVALID_CREDENTIALS = "AUTH_INVALID_CREDENTIALS";
    public const string AUTH_TOKEN_INVALID = "AUTH_TOKEN_INVALID";
    public const string AUTH_TOKEN_EXPIRED = "AUTH_TOKEN_EXPIRED";
    public const string AUTH_FORBIDDEN = "AUTH_FORBIDDEN";

    // Validation errors
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";

    // Leave errors
    public const string INSUFFICIENT_BALANCE = "INSUFFICIENT_BALANCE";
    public const string OVERLAPPING_LEAVE = "OVERLAPPING_LEAVE";
    public const string INVALID_START_DATE = "INVALID_START_DATE";
    public const string INVALID_END_DATE = "INVALID_END_DATE";
    public const string INVALID_STATUS_TRANSITION = "INVALID_STATUS_TRANSITION";
    public const string LEAVE_NOT_FOUND = "LEAVE_NOT_FOUND";

    // Employee errors
    public const string EMPLOYEE_NOT_FOUND = "EMPLOYEE_NOT_FOUND";
    public const string INVALID_LEAVE_TYPE = "INVALID_LEAVE_TYPE";

    // System errors
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
}
