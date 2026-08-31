using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EBOSP.Worker;

/// <summary>
/// The worker has no HTTP endpoint to expose a /health route, so container health is checked via
/// a heartbeat file instead: refreshed on every healthy check cycle by
/// <see cref="HeartbeatBackgroundService"/>, and the Docker HEALTHCHECK
/// (infra/docker/worker.Dockerfile) fails once the file goes stale (dev guide §30: add health
/// checks; §29: track worker health).
/// </summary>
public sealed class HeartbeatHealthCheckPublisher(ILogger<HeartbeatHealthCheckPublisher> logger)
    : IHealthCheckPublisher
{
    public static readonly string HeartbeatFilePath =
        Environment.GetEnvironmentVariable("HEARTBEAT_FILE_PATH") ?? "/tmp/worker-healthy";

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        if (report.Status != HealthStatus.Healthy)
        {
            logger.LogWarning("Worker health check reported {Status}; heartbeat not refreshed.", report.Status);
            return Task.CompletedTask;
        }

        try
        {
            File.WriteAllText(HeartbeatFilePath, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Failed to write worker heartbeat file at {Path}.", HeartbeatFilePath);
        }

        return Task.CompletedTask;
    }
}
