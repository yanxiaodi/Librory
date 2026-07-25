using Librory.Domain.Models;

namespace Librory.Application.Scanning;

public interface IScanSessionService
{
    Task<ScanSessionDto> StartShelfScanAsync(ScanShelfRequest request, CancellationToken cancellationToken);

    Task<ScanSessionDto> ApplyCorrectionAsync(Guid scanSessionId, Guid candidateId, CorrectionRequest request, CancellationToken cancellationToken);

    Task<BookWork> ResolveCandidateAsync(
        Guid scanSessionId,
        Guid candidateId,
        string title,
        string? author,
        string? isbn,
        string? format,
        int? publicationYear,
        CancellationToken cancellationToken);

    Task DiscardCandidateAsync(Guid scanSessionId, Guid candidateId, CancellationToken cancellationToken);
}
