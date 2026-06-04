using Serilog;
using Shared.Common.Middleware;
using Shared.Common.Configuration;
using Shared.Common.Extensions;
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

// ========== LOGGING CONFIGURATION ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "NotificationService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/notificationservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ========== SERVICE REGISTRATION ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCommonServices();

// ========== JWT AUTHENTICATION ==========
builder.Services.AddJwtAuthentication(builder.Configuration);

// ========== RABBITMQ CONFIGURATION ==========
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

// ========== NOTIFICATION SERVICES ==========
builder.Services.AddSingleton<INotificationSender, ConsoleNotificationSender>();
builder.Services.AddSingleton<NotificationDataStore>();

// ========== RABBITMQ CONSUMER (Background Service) ==========
builder.Services.AddHostedService<RabbitMQConsumerService>();

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
app.UseMiddleware<HealthCheckMiddleware>("NotificationService");
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting Notification Service on port 5004");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Notification Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
