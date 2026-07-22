namespace Librory.Application.Scanning;

public interface IScanSessionService
{
    Task<ScanSessionDto> StartShelfScanAsync(ScanShelfRequest request, CancellationToken cancellationToken);

    Task<ScanSessionDto> ApplyCorrectionAsync(Guid scanSessionId, Guid candidateId, CorrectionRequest request, CancellationToken cancellationToken);
}
