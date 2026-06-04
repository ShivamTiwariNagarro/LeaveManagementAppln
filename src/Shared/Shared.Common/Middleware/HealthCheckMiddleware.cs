using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Shared.Common.Middleware;

/// <summary>
/// Lightweight health check middleware that responds to /health endpoint.
/// 
/// Purpose:
/// - Provides quick health status endpoint for monitoring tools (Docker, Kubernetes, load balancers)
/// - Returns service name, status, and timestamp
/// - Does not check database connections or external dependencies (fast response)
/// - Used in docker-compose.yml healthcheck and by monitoring systems
/// 
/// Response example:
/// {
///   "status": "Healthy",
///   "service": "AuthService",
///   "timestamp": "2026-05-31T10:30:00Z"
/// }
/// </summary>
public class HealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _serviceName;

    public HealthCheckMiddleware(RequestDelegate next, string serviceName)
    {
        _next = next;
        _serviceName = serviceName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Intercept /health requests and return status immediately
        // Short-circuits the pipeline - other middleware won't execute for health checks
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;

            var response = new
            {
                status = "Healthy",
                service = _serviceName,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
            return;
        }

        // Not a health check request, continue to next middleware
        await _next(context);
    }
}
