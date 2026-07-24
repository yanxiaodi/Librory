namespace Librory.Domain.Models;

public sealed record MetadataProvenance
{
    public string? Source { get; init; }

    public string? SourceId { get; init; }

    public decimal? Confidence { get; init; }

    public DateTimeOffset? CapturedAt { get; init; }

    public MetadataProvenance(
        string? source = null,
        string? sourceId = null,
        decimal? confidence = null,
        DateTimeOffset? capturedAt = null)
    {
        if (confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidence),
                confidence,
                "Confidence must be between 0 and 1.");
        }

        Source = source;
        SourceId = sourceId;
        Confidence = confidence;
        CapturedAt = capturedAt;
    }

    public MetadataProvenance()
    {
    }
}
