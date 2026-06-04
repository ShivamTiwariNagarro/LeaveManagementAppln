using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Shared.Common.Middleware;

/// <summary>
/// Middleware to propagate correlation IDs across service calls
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    
    public const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = GetOrGenerateCorrelationId(context);

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            }
            return Task.CompletedTask;
        });

        // Store in HttpContext for access by other components
        context.Items["CorrelationId"] = correlationId;

        // Push to Serilog context for structured logging
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogDebug("Processing request with CorrelationId: {CorrelationId}", correlationId);
            await _next(context);
        }
    }

    private string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Check for existing correlation ID in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingId) 
            && !string.IsNullOrWhiteSpace(existingId))
        {
            return existingId.ToString();
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString("N")[..12];
    }
}

/// <summary>
/// Extension methods for correlation ID access
/// </summary>
public static class CorrelationIdExtensions
{
    public static string? GetCorrelationId(this HttpContext context)
    {
        return context.Items.TryGetValue("CorrelationId", out var correlationId) 
            ? correlationId?.ToString() 
            : null;
    }
}
