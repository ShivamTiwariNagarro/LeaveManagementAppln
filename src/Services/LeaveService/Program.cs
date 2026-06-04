using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Shared.Common.Extensions;
using Shared.Common.Middleware;
using Shared.Common.Messaging;
using Shared.Common.Configuration;
using LeaveService.Data;
using LeaveService.Services;

var builder = WebApplication.CreateBuilder(args);

// ========== LOGGING CONFIGURATION ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "LeaveService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/leaveservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ========== SERVICE REGISTRATION ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// ========== DATA STORES ==========
builder.Services.AddSingleton<LeaveRequestDataStore>();

// ========== SERVICES ==========
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestServiceImpl>();

// ========== RABBITMQ ==========
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<RabbitMQPublisher>();

// ========== HTTP CLIENT FOR EMPLOYEE SERVICE (with Polly Circuit Breaker) ==========
builder.Services.AddHttpClient("EmployeeService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:EmployeeService"] ?? "http://localhost:5002");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(300 * retryAttempt)))
.AddTransientHttpErrorPolicy(p =>
    p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

// ========== JWT AUTHENTICATION ==========
builder.Services.AddJwtAuthentication(builder.Configuration);

// ========== SERVICE DISCOVERY ==========
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

// ========== CORS CONFIGURATION ==========
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Internal service - requests come from Gateway, not browsers
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ========== HTTP PIPELINE ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<HealthCheckMiddleware>("LeaveService");
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting Leave Service on port 5003");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Leave Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
