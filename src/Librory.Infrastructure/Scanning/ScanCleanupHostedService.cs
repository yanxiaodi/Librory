using Librory.Application.Scanning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Librory.Infrastructure.Scanning;

public sealed class ScanCleanupHostedService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScanCleanupHostedService> _logger;

    public ScanCleanupHostedService(IServiceScopeFactory scopeFactory, ILogger<ScanCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Each app instance runs this hosted service. If the API scales horizontally,
        // cleanup can overlap across instances until we add distributed coordination.
        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IScanSessionCleanupService>();
        var deletedCount = await cleanupService.DeleteExpiredTemporaryScanDataAsync(DateTimeOffset.UtcNow, stoppingToken);
        _logger.LogInformation("Scan cleanup removed {DeletedCount} expired scan sessions.", deletedCount);
    }
}
