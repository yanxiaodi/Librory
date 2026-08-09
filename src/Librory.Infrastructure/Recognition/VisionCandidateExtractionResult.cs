using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Librory.Infrastructure.Recognition;

/// <summary>
/// JSON-schema contract requested from the vision-capable LLM. Kept separate from
/// <see cref="Librory.Application.Recognition.BookCandidate"/> so the agent's structured-output
/// shape can evolve independently of the application-layer candidate contract.
/// </summary>
[Description("Structured book candidates extracted from a shelf photo.")]
public sealed class VisionCandidateExtractionResult
{
    [JsonPropertyName("candidates")]
    [Description("Candidate books visible in the photo, ordered by confidence descending.")]
    public List<VisionBookCandidate> Candidates { get; set; } = [];
}

public sealed class VisionBookCandidate
{
    [JsonPropertyName("title")]
    [Description("The book title as read from the spine or cover.")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    [Description("The author name if visible near the title, otherwise null.")]
    public string? Author { get; set; }

    [JsonPropertyName("evidenceText")]
    [Description("The exact text read from the image that supports this candidate.")]
    public string? EvidenceText { get; set; }

    [JsonPropertyName("confidence")]
    [Description("Confidence between 0 and 1 that this is a genuine book title, not noise.")]
    public double Confidence { get; set; }
}
