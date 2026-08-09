namespace Librory.Application.Recognition;

public sealed record BookCandidate(
    string Title,
    string? Author,
    string? EvidenceText,
    decimal Confidence);