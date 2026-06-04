using System.Diagnostics;

namespace Shared.Common.DTOs;

/// <summary>
/// Standard API response wrapper for all endpoints
/// </summary>
/// <typeparam name="T">Type of data payload</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? TraceId { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Request completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            TraceId = Activity.Current?.Id ?? Activity.Current?.TraceId.ToString()
        };
    }

    public static ApiResponse<T> FailResponse(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            TraceId = Activity.Current?.Id ?? Activity.Current?.TraceId.ToString()
        };
    }

    // Shorthand methods
    public static ApiResponse<T> Ok(T data, string message = "Request completed successfully") 
        => SuccessResponse(data, message);
    
    public static ApiResponse<T> Fail(string message) 
        => FailResponse(message);
}

/// <summary>
/// Non-generic API response for simple success/failure responses
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }

    public static ApiResponse SuccessResponse(string message = "Request completed successfully")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            TraceId = Activity.Current?.Id ?? Activity.Current?.TraceId.ToString()
        };
    }

    public static ApiResponse FailResponse(string message)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            TraceId = Activity.Current?.Id ?? Activity.Current?.TraceId.ToString()
        };
    }

    // Shorthand methods
    public static ApiResponse Ok(string message = "Request completed successfully") 
        => SuccessResponse(message);
    
    public static ApiResponse Fail(string message) 
        => FailResponse(message);
}
