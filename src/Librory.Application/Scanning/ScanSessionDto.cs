using Librory.Domain.Models;

namespace Librory.Application.Scanning;

public sealed record ScanSessionDto(
    Guid ScanSessionId,
    Guid FamilyId,
    string ShelfPhotoPath,
    IReadOnlyList<ScanCandidateDto> Candidates,
    DateTimeOffset ExpiresAt,
    Guid? TargetMemberId = null,
    string TargetMemberDisplayName = "",
    bool TargetProfileAvailable = false,
    bool TargetProfileUsed = false,
    PreferredLanguage? InferredLanguage = null,
    bool HasMixedLanguages = false);
