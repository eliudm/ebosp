using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EBOSP.Worker;

/// <summary>
/// Runs the registered health checks on a fixed interval and hands the result to every
/// <see cref="IHealthCheckPublisher"/> (currently just <see cref="HeartbeatHealthCheckPublisher"/>).
/// A generic Host has no built-in equivalent of ASP.NET Core's /health endpoint, so this is the
/// worker's own periodic health-check runner (dev guide §29, §30).
/// </summary>
public sealed class HeartbeatBackgroundService(
    HealthCheckService healthCheckService,
    IEnumerable<IHealthCheckPublisher> publishers,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, timeProvider);
        do
        {
            var report = await healthCheckService.CheckHealthAsync(stoppingToken);
            await Task.WhenAll(publishers.Select(publisher => publisher.PublishAsync(report, stoppingToken)));
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
