using System.Text.Json;
using Librory.Application.Recognition;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Librory.Infrastructure.Recognition;

public sealed class BookRecognitionJobProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxFailureMessageLength = 2_000;
    private const int BatchSize = 5;

    private readonly LibroryDbContext _db;
    private readonly IBookRecognitionPipeline _pipeline;
    private readonly ILogger<BookRecognitionJobProcessor> _logger;

    public BookRecognitionJobProcessor(
        LibroryDbContext db,
        IBookRecognitionPipeline pipeline,
        ILogger<BookRecognitionJobProcessor> logger)
    {
        _db = db;
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task<int> ProcessQueuedJobsAsync(CancellationToken cancellationToken)
    {
        var queuedJobs = await ClaimQueuedJobsAsync(cancellationToken);

        if (queuedJobs.Count == 0)
        {
            _logger.LogDebug("No queued book recognition jobs were available in this sweep.");
            return 0;
        }

        _logger.LogInformation("Claimed {JobCount} queued book recognition job(s) for processing.", queuedJobs.Count);

        var processed = 0;
        foreach (var job in queuedJobs)
        {
            _logger.LogInformation(
                "Processing book recognition job {JobId} for family {FamilyId} from {SourcePhotoPath}.",
                job.Id,
                job.FamilyId,
                job.SourcePhotoPath);

            job.MarkRunning(DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var result = await _pipeline.RecognizeAsync(job.SourcePhotoPath, job.Language, cancellationToken);
                job.MarkSucceeded(JsonSerializer.Serialize(result, JsonOptions), DateTimeOffset.UtcNow);
                _logger.LogInformation(
                    "Book recognition job {JobId} succeeded with {CandidateCount} candidate(s) and {WarningCount} warning(s).",
                    job.Id,
                    result.Candidates.Count,
                    result.Warnings.Count);
            }
            catch (Exception exception)
            {
                job.MarkFailed(CreateFailureMessage(exception), DateTimeOffset.UtcNow);
                _logger.LogWarning(
                    exception,
                    "Book recognition job {JobId} failed for family {FamilyId}.",
                    job.Id,
                    job.FamilyId);
            }

            await _db.SaveChangesAsync(cancellationToken);
            processed++;
        }

        return processed;
    }

    private async Task<List<BookRecognitionJob>> ClaimQueuedJobsAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var queuedJobs = await _db.BookRecognitionJobs
            .FromSqlInterpolated($"""
                SELECT * FROM librory.book_recognition_jobs
                WHERE "Status" = {(int)BookRecognitionJobStatus.Queued}
                ORDER BY "CreatedAt"
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        if (queuedJobs.Count == 0)
        {
            _logger.LogDebug("No queued book recognition jobs were found to claim.");
            return queuedJobs;
        }

        _logger.LogInformation("Claiming {JobCount} queued book recognition job(s) from the database.", queuedJobs.Count);

        var now = DateTimeOffset.UtcNow;
        foreach (var job in queuedJobs)
        {
            job.MarkRunning(now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return queuedJobs;
    }

    private static string CreateFailureMessage(Exception exception)
    {
        var message = exception.Message.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Recognition failed.";
        }

        if (message.Length > MaxFailureMessageLength)
        {
            return message[..MaxFailureMessageLength].Trim();
        }

        return message;
    }
}
