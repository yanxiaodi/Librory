using System.Text.Json;
using Librory.Application.Metadata;

namespace Librory.Application.Scanning;

public sealed record ScanCandidateMetadataSnapshot(
    int SchemaVersion,
    string? EvidenceText,
    IReadOnlyList<BookMetadataCandidate> Matches);

public static class ScanCandidateMetadataSnapshotSerializer
{
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize(
        IReadOnlyList<BookMetadataCandidate>? matches,
        string? evidenceText)
    {
        var normalizedMatches = (matches ?? [])
            .Where(match => match is not null)
            .ToArray();

        return JsonSerializer.Serialize(
            new ScanCandidateMetadataSnapshot(CurrentSchemaVersion, Normalize(evidenceText), normalizedMatches),
            Options);
    }

    public static ScanCandidateMetadataSnapshot? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize<ScanCandidateMetadataSnapshot>(json, Options);
            return snapshot is { SchemaVersion: CurrentSchemaVersion }
                ? snapshot
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
