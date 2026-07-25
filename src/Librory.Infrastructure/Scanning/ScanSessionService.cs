using Librory.Application.Families;
using Librory.Application.Scanning;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Infrastructure.Scanning;

public sealed class ScanSessionService : IScanSessionService
{
    private readonly LibroryDbContext _db;
    private readonly ICurrentFamilyContextAccessor _currentFamilyContextAccessor;

    public ScanSessionService(
        LibroryDbContext db,
        ICurrentFamilyContextAccessor currentFamilyContextAccessor)
    {
        _db = db;
        _currentFamilyContextAccessor = currentFamilyContextAccessor;
    }

    public async Task<ScanSessionDto> StartShelfScanAsync(
        ScanShelfRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var current = RequireCurrentContext();
        if (request.FamilyId != current.FamilyId)
        {
            throw new InvalidOperationException("Scan request family id must match the active family context.");
        }

        var family = await LoadFamilyForDuplicateDetectionAsync(request.FamilyId, cancellationToken);
        if (family is null)
        {
            throw new KeyNotFoundException("Family not found.");
        }

        var session = ScanSessionRecorder.Record(family, request);
        _db.ScanSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return ScanSessionDtoFactory.Create(family, session);
    }

    public async Task<ScanSessionDto> ApplyCorrectionAsync(
        Guid scanSessionId,
        Guid candidateId,
        CorrectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var current = RequireCurrentContext();
        var family = await LoadFamilyForDuplicateDetectionAsync(current.FamilyId, cancellationToken);
        if (family is null)
        {
            throw new KeyNotFoundException("Family not found.");
        }

        var session = await LoadScanSessionAsync(current.FamilyId, scanSessionId, cancellationToken);
        if (session is null || session.IsExpired())
        {
            throw new KeyNotFoundException("Scan session not found.");
        }

        var candidate = session.Candidates.SingleOrDefault(existing => existing.Id == candidateId);
        if (candidate is null)
        {
            throw new KeyNotFoundException("Scan candidate not found.");
        }

        candidate.ApplyCorrection(
            request.DisplayTitle,
            request.ConfidenceLabel,
            request.Author,
            request.RecommendationScore,
            request.IsAlreadyOwned,
            request.DuplicateMessage);

        await _db.SaveChangesAsync(cancellationToken);

        return ScanSessionDtoFactory.Create(family, session);
    }

    private CurrentFamilyContext RequireCurrentContext()
    {
        var current = _currentFamilyContextAccessor.Current;
        if (current is null)
        {
            throw new UnauthorizedAccessException("Current family context is required.");
        }

        return current;
    }

    private Task<Family?> LoadFamilyForDuplicateDetectionAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return _db.Families
            .Include(x => x.BookCopies)
                .ThenInclude(x => x.BookEdition)
                    .ThenInclude(x => x.BookWork)
            .SingleOrDefaultAsync(x => x.Id == familyId, cancellationToken);
    }

    private Task<ScanSession?> LoadScanSessionAsync(
        Guid familyId,
        Guid scanSessionId,
        CancellationToken cancellationToken)
    {
        return _db.ScanSessions
            .Include(x => x.Candidates)
            .SingleOrDefaultAsync(x => x.FamilyId == familyId && x.Id == scanSessionId, cancellationToken);
    }
}
