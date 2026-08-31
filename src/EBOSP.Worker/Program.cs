using EBOSP.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IHealthCheckPublisher, HeartbeatHealthCheckPublisher>();
builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Period = TimeSpan.FromSeconds(10);
});

var host = builder.Build();
host.Run();
