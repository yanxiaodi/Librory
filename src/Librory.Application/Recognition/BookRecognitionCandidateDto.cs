using Librory.Application.Metadata;

namespace Librory.Application.Recognition;

public sealed record BookRecognitionCandidateDto(
    Guid CandidateId,
    string DisplayTitle,
    string EvidenceText,
    int Rank,
    IReadOnlyList<BookMetadataCandidate> MetadataMatches);
