using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Shared.Common.Middleware;
using Serilog;
using Steeltoe.Discovery.Client;

namespace Shared.Common.Extensions;

/// <summary>
/// Extension methods for service collection configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add common services used across all microservices
    /// </summary>
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        // Add HTTP context accessor for correlation ID access
        services.AddHttpContextAccessor();
        
        return services;
    }

    /// <summary>
    /// Add JWT authentication configuration
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secretKey = jwtSection["SecretKey"] ?? throw new ArgumentNullException("Jwt:SecretKey is required");
        var issuer = jwtSection["Issuer"] ?? "leave-management-system";
        var audience = jwtSection["Audience"] ?? "leave-management-api";

        // Register JwtSettings for IOptions<JwtSettings> injection
        services.Configure<Shared.Common.Configuration.JwtSettings>(jwtSection);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var userId = context.Principal?.FindFirst("sub")?.Value;
                    Log.Debug("JWT token validated for user: {UserId}", userId);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Configure common middleware pipeline
    /// </summary>
    public static IApplicationBuilder UseCommonMiddleware(this IApplicationBuilder app)
    {
        // Correlation ID should be first to track all requests
        app.UseMiddleware<CorrelationIdMiddleware>();
        
        // Exception handling wraps all subsequent middleware
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        
        return app;
    }

    /// <summary>
    /// Add Consul service discovery configuration
    /// </summary>
    public static IServiceCollection AddConsulServiceDiscovery(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDiscoveryClient(configuration);
        return services;
    }

    /// <summary>
    /// Add JWT token generator service (for AuthService)
    /// </summary>
    public static IServiceCollection AddJwtTokenGenerator(this IServiceCollection services)
    {
        services.AddScoped<Shared.Common.Utilities.JwtTokenGenerator>();
        return services;
    }
}
