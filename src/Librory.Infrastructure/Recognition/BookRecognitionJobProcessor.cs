using System.Text.Json;
using Librory.Application.Recognition;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Infrastructure.Recognition;

public sealed class BookRecognitionJobProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
        var queuedJobs = await _db.BookRecognitionJobs
            .Where(job => job.Status == BookRecognitionJobStatus.Queued)
            .OrderBy(job => job.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

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
                job.MarkFailed(exception.Message, DateTimeOffset.UtcNow);
            }

            await _db.SaveChangesAsync(cancellationToken);
            processed++;
        }

        return processed;
    }
}
