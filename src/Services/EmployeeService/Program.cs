using Serilog;
using Shared.Common.Extensions;
using Shared.Common.Middleware;
using EmployeeService.Data;
using EmployeeService.Services;

var builder = WebApplication.CreateBuilder(args);

// ========== LOGGING CONFIGURATION ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "EmployeeService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/employeeservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ========== SERVICE REGISTRATION ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========== DATA STORES ==========
builder.Services.AddSingleton<EmployeeDataStore>();
builder.Services.AddSingleton<LeaveBalanceDataStore>();

// ========== SERVICES ==========
builder.Services.AddScoped<IEmployeeService, EmployeeServiceImpl>();
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceServiceImpl>();

// ========== JWT AUTHENTICATION ==========
builder.Services.AddJwtAuthentication(builder.Configuration);

// ========== SERVICE DISCOVERY ==========
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

// ========== CORS CONFIGURATION ==========
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Internal service - requests come from Gateway/other services, not browsers
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
app.UseMiddleware<HealthCheckMiddleware>("EmployeeService");
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting Employee Service on port 5002");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Employee Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
