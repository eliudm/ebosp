using EBOSP.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IHealthCheckPublisher, HeartbeatHealthCheckPublisher>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<HeartbeatBackgroundService>();

var host = builder.Build();
host.Run();
