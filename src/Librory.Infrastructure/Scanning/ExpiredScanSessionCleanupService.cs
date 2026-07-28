using Librory.Application.Scanning;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Librory.Infrastructure.Scanning;

public sealed class ExpiredScanSessionCleanupService : IScanSessionCleanupService
{
    private readonly LibroryDbContext _db;
    private readonly IScanPhotoStorage _photoStorage;
    private readonly ILogger<ExpiredScanSessionCleanupService> _logger;

    public ExpiredScanSessionCleanupService(
        LibroryDbContext db,
        IScanPhotoStorage photoStorage,
        ILogger<ExpiredScanSessionCleanupService> logger)
    {
        _db = db;
        _photoStorage = photoStorage;
        _logger = logger;
    }

    public async Task<int> DeleteExpiredTemporaryScanDataAsync(DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        const int batchSize = 100;
        var deletedCount = 0;

        while (true)
        {
            var expiredSessions = await _db.ScanSessions
                .Where(session => session.ExpiresAt <= asOf)
                .OrderBy(session => session.ExpiresAt)
                .ThenBy(session => session.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (expiredSessions.Count == 0)
            {
                break;
            }

            foreach (var session in expiredSessions)
            {
                try
                {
                    await _photoStorage.DeleteAsync(session.ShelfPhotoPath, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Failed to delete scan photo for expired session {ScanSessionId} at {ShelfPhotoPath}.",
                        session.Id,
                        session.ShelfPhotoPath);
                }

                _db.ScanSessions.Remove(session);
                deletedCount++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return deletedCount;
    }
}
