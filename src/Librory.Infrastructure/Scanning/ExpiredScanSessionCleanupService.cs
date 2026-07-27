using Librory.Application.Scanning;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Infrastructure.Scanning;

public sealed class ExpiredScanSessionCleanupService : IScanSessionCleanupService
{
    private readonly LibroryDbContext _db;
    private readonly IScanPhotoStorage _photoStorage;

    public ExpiredScanSessionCleanupService(LibroryDbContext db, IScanPhotoStorage photoStorage)
    {
        _db = db;
        _photoStorage = photoStorage;
    }

    public async Task<int> DeleteExpiredTemporaryScanDataAsync(DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        var expiredSessions = await _db.ScanSessions
            .Where(session => session.ExpiresAt <= asOf)
            .ToListAsync(cancellationToken);

        if (expiredSessions.Count == 0)
        {
            return 0;
        }

        foreach (var session in expiredSessions)
        {
            await _photoStorage.DeleteAsync(session.ShelfPhotoPath, cancellationToken);
            _db.ScanSessions.Remove(session);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return expiredSessions.Count;
    }
}
