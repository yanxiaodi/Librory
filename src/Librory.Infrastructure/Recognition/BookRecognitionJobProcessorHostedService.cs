using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Librory.Infrastructure.Recognition;

public sealed class BookRecognitionJobProcessorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookRecognitionJobProcessorHostedService> _logger;

    public BookRecognitionJobProcessorHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookRecognitionJobProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        _logger.LogInformation("Book recognition job processor hosted service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<BookRecognitionJobProcessor>();
                var processed = await processor.ProcessQueuedJobsAsync(stoppingToken);
                _logger.LogDebug("Book recognition job processor sweep completed with {ProcessedCount} processed job(s).", processed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Book recognition job processing sweep failed.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
