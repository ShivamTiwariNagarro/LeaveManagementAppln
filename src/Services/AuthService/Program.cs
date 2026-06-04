using Serilog;
using Shared.Common.Extensions;
using Shared.Common.Middleware;
using AuthService.Data;
using AuthService.Services;

var builder = WebApplication.CreateBuilder(args);

// ========== LOGGING CONFIGURATION ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "AuthService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/authservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ========== SERVICE REGISTRATION ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========== DATA STORE ==========
// In-memory user data store with pre-seeded users
builder.Services.AddSingleton<UserDataStore>();

// ========== AUTH SERVICE ==========
builder.Services.AddScoped<IAuthService, AuthServiceImpl>();

// ========== JWT TOKEN GENERATION ==========
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddJwtTokenGenerator();

// ========== SERVICE DISCOVERY ==========
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

// ========== CORS CONFIGURATION ==========
// Note: CORS is minimal here since requests come through API Gateway, not directly from browsers
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
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
app.UseMiddleware<HealthCheckMiddleware>("AuthService");
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting Auth Service on port 5001");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Auth Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
