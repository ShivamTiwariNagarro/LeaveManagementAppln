using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Common.DTOs;
using System.Net;
using System.Text.Json;

namespace Shared.Common.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        
        _logger.LogError(exception, 
            "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}, Method: {Method}",
            traceId, context.Request.Path, context.Request.Method);

        var (statusCode, errorCode, message) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ErrorCodes.AUTH_FORBIDDEN, "Access denied"),
            ArgumentException argEx => (HttpStatusCode.BadRequest, ErrorCodes.VALIDATION_ERROR, argEx.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", "Resource not found"),
            InvalidOperationException invEx => (HttpStatusCode.BadRequest, ErrorCodes.VALIDATION_ERROR, invEx.Message),
            _ => (HttpStatusCode.InternalServerError, ErrorCodes.INTERNAL_ERROR, "An unexpected error occurred")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var errorResponse = ErrorResponse.Create(message, errorCode, traceId);

        var options = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}
