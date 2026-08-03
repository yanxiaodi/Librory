using System.Text.Json;
using Librory.Application.Recognition;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Infrastructure.Recognition;

public sealed class BookRecognitionJobProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxFailureMessageLength = 2_000;
    private const int BatchSize = 5;

    private readonly LibroryDbContext _db;
    private readonly IBookRecognitionPipeline _pipeline;

    public BookRecognitionJobProcessor(LibroryDbContext db, IBookRecognitionPipeline pipeline)
    {
        _db = db;
        _pipeline = pipeline;
    }

    public async Task<int> ProcessQueuedJobsAsync(CancellationToken cancellationToken)
    {
        var queuedJobs = await ClaimQueuedJobsAsync(cancellationToken);

        if (queuedJobs.Count == 0)
        {
            return 0;
        }

        var processed = 0;
        foreach (var job in queuedJobs)
        {
            job.MarkRunning(DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var result = await _pipeline.RecognizeAsync(job.SourcePhotoPath, job.Language, cancellationToken);
                job.MarkSucceeded(JsonSerializer.Serialize(result, JsonOptions), DateTimeOffset.UtcNow);
            }
            catch (Exception exception)
            {
                job.MarkFailed(CreateFailureMessage(exception), DateTimeOffset.UtcNow);
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
            return queuedJobs;
        }

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
