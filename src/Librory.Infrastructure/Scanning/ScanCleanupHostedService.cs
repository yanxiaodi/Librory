using Librory.Application.Scanning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Librory.Infrastructure.Scanning;

public sealed class ScanCleanupHostedService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(1);
    private readonly IServiceScopeFactory _scopeFactory;

    public ScanCleanupHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        await cleanupService.DeleteExpiredTemporaryScanDataAsync(DateTimeOffset.UtcNow, stoppingToken);
    }
}
