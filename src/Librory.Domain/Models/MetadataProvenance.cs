namespace Librory.Domain.Models;

public sealed record MetadataProvenance
{
    public string? Source { get; init; }

    public string? SourceId { get; init; }

    public decimal? Confidence { get; init; }

    public DateTimeOffset? CapturedAt { get; init; }
}
