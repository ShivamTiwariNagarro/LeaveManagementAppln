using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Serilog;
using Shared.Common.Extensions;
using Shared.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ========== OCELOT CONFIGURATION SELECTION ==========
// Select Ocelot config based on environment: Docker uses container hostnames, local uses localhost
var environment = builder.Environment.EnvironmentName;
var ocelotConfigFile = environment == "Docker" ? "ocelot.docker.json" : "ocelot.json";

// Load the selected Ocelot configuration file
builder.Configuration.AddJsonFile(ocelotConfigFile, optional: false, reloadOnChange: true);
Log.Information($"Loading Ocelot config: {ocelotConfigFile} for environment: {environment}");

// ========== LOGGING CONFIGURATION ==========
// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "ApiGateway")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/apigateway-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ========== JWT AUTHENTICATION ==========
// Add JWT Bearer authentication using shared configuration extension
builder.Services.AddJwtAuthentication(builder.Configuration);

// ========== OCELOT API GATEWAY SETUP ==========
// Add Ocelot with Consul for dynamic service discovery
// Routes with "ServiceName" will query Consul to find service instances
// instead of using hardcoded DownstreamHostAndPorts
var ocelotBuilder = builder.Services
    .AddOcelot()
    .AddConsul();

// ========== CORS CONFIGURATION ==========
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",              // Frontend (local React/Angular)
                "http://localhost:8080",              // Frontend (alternate port)
                "https://leavemanagement.company.com" // Production domain
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ========== MIDDLEWARE PIPELINE ==========
app.UseCors();

// Add correlation ID for distributed tracing
app.UseMiddleware<CorrelationIdMiddleware>();

// Health check endpoint
app.UseMiddleware<HealthCheckMiddleware>("ApiGateway");

// Authentication must be before Ocelot
app.UseAuthentication();

// Ocelot gateway middleware (handles all routing)
await app.UseOcelot();

try
{
    Log.Information("Starting API Gateway on port 5000");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
