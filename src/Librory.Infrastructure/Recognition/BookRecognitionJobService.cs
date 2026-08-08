using System.Text.Json;
using Librory.Application.Metadata;
using Librory.Application.Recognition;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Infrastructure.Recognition;

public sealed class BookRecognitionJobService : IBookRecognitionJobService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LibroryDbContext _db;

    public BookRecognitionJobService(LibroryDbContext db)
    {
        _db = db;
    }

    public async Task<BookRecognitionJobDto> CreateAsync(
        Guid familyId,
        string sourcePhotoPath,
        string? language,
        CancellationToken cancellationToken)
    {
        var familyExists = await _db.Families.AnyAsync(x => x.Id == familyId, cancellationToken);
        if (!familyExists)
        {
            throw new KeyNotFoundException("Family not found.");
        }

        var job = Domain.Models.BookRecognitionJob.Create(familyId, sourcePhotoPath, language, DateTimeOffset.UtcNow);
        _db.Set<Domain.Models.BookRecognitionJob>().Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(job);
    }

    public async Task<BookRecognitionJobDto?> GetAsync(
        Guid familyId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await _db.Set<Domain.Models.BookRecognitionJob>()
            .SingleOrDefaultAsync(x => x.FamilyId == familyId && x.Id == jobId, cancellationToken);

        return job is null ? null : ToDto(job);
    }

    private static BookRecognitionJobDto ToDto(Domain.Models.BookRecognitionJob job)
    {
        var result = DeserializeResult(job.ResultJson, out var resultWarning);
        var candidates = result?.Candidates ?? [];
        var warnings = result?.Warnings ?? [];

        if (resultWarning is not null)
        {
            warnings = [.. warnings, resultWarning];
        }

        return new BookRecognitionJobDto(
            job.Id,
            job.FamilyId,
            job.Status,
            job.SourcePhotoPath,
            candidates,
            warnings,
            job.FailureMessage,
            job.CreatedAt,
            job.UpdatedAt);
    }

    private static BookRecognitionJobResult? DeserializeResult(string? resultJson, out string? warning)
    {
        warning = null;

        if (string.IsNullOrWhiteSpace(resultJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BookRecognitionJobResult>(resultJson, JsonOptions);
        }
        catch (JsonException)
        {
            warning = "Stored recognition result payload could not be parsed.";
            return null;
        }
    }
}
