using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record ScanSessionResponse(
    Guid ScanSessionId,
    Guid FamilyId,
    string ShelfPhotoPath,
    IReadOnlyList<ScanCandidateResponse> Candidates,
    DateTimeOffset ExpiresAt,
    Guid? TargetMemberId = null,
    string TargetMemberDisplayName = "",
    bool TargetProfileAvailable = false,
    bool TargetProfileUsed = false,
    PreferredLanguage? InferredLanguage = null,
    bool HasMixedLanguages = false);
