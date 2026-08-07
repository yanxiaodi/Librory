namespace Librory.Application.Scanning;

public sealed record ScanTargetContext(
    Guid TargetMemberId,
    bool TargetProfileAvailable,
    bool TargetProfileUsed);
