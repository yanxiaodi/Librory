namespace Librory.Application.Scanning;

public interface IScanSessionCleanupService
{
    Task<int> DeleteExpiredTemporaryScanDataAsync(DateTimeOffset asOf, CancellationToken cancellationToken);
}
