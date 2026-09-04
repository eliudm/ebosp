using EBOSP.Application.Common;
using EBOSP.Infrastructure.Common;
using EBOSP.Infrastructure.Persistence;
using EBOSP.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing required configuration: ConnectionStrings:Default. " +
        "Set it via user-secrets locally or environment/Key Vault in deployed environments.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<IClock, SystemClock>();
// No HttpContext exists in a Generic Host - every tenant-scoped query the processor runs must use
// IgnoreQueryFilters() plus an explicit tenant Where instead of relying on this.
builder.Services.AddScoped<ICurrentUserContext, NullCurrentUserContext>();

builder.Services.AddHostedService<NotificationOutboxProcessor>();

builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IHealthCheckPublisher, HeartbeatHealthCheckPublisher>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<HeartbeatBackgroundService>();

var host = builder.Build();
host.Run();
