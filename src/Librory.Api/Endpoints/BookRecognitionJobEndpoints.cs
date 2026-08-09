using Librory.Api.Contracts;
using Librory.Api.Validation;
using Librory.Application.Families;
using Librory.Application.Recognition;
using Librory.Application.Scanning;
using Librory.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Librory.Api.Endpoints;

internal static class BookRecognitionJobEndpoints
{
    public static IEndpointRouteBuilder MapBookRecognitionJobEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/book-recognition-jobs")
            .RequireAuthorization()
            .WithTags("Recognition");

        group.MapPost(string.Empty, CreateBookRecognitionJobAsync)
            .WithName("CreateBookRecognitionJob")
            .WithSummary("Create a book recognition job.")
            .WithDescription("Stores an uploaded photo temporarily and creates an async job that will extract book-title candidates.")
            .Produces<BookRecognitionJobResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("{jobId:guid}", GetBookRecognitionJobAsync)
            .WithName("GetBookRecognitionJob")
            .WithSummary("Get a book recognition job.")
            .WithDescription("Returns the stored recognition job and its candidate results for the current family.")
            .Produces<BookRecognitionJobResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateBookRecognitionJobAsync(
        HttpRequest request,
        ICurrentFamilyContextAccessor accessor,
        IScanPhotoStorage photoStorage,
        IBookRecognitionJobService recognitionJobService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Librory.Api.Endpoints.BookRecognitionJobEndpoints");
        logger.LogInformation(
            "Book recognition upload endpoint invoked. Path={Path}, Method={Method}, ContentLength={ContentLength}, ContentType={ContentType}.",
            request.Path,
            request.Method,
            request.ContentLength,
            request.ContentType ?? "<unknown>");

        var current = accessor.Current;
        if (current is null)
        {
            logger.LogWarning("Rejected book recognition upload request because no current family context was available.");
            return Results.Unauthorized();
        }

        logger.LogInformation(
            "Received book recognition upload request for family {FamilyId}.",
            current.FamilyId);

        logger.LogInformation(
            "Book recognition upload request for family {FamilyId} reached multipart validation with content length {ContentLength} and content type {ContentType}.",
            current.FamilyId,
            request.ContentLength,
            request.ContentType ?? "<unknown>");

        if (request.ContentLength is null || request.ContentLength == 0 || !request.HasFormContentType)
        {
            logger.LogWarning("Rejected book recognition upload request for family {FamilyId} because the request body was missing or not multipart form data.", current.FamilyId);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["photo"] = ["Recognition photo is required."],
            });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var photo = form.Files.GetFile("photo");
        if (photo is null || photo.Length <= 0)
        {
            logger.LogWarning("Rejected book recognition upload request for family {FamilyId} because no photo file was provided.", current.FamilyId);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["photo"] = ["Recognition photo is required."],
            });
        }

        if (!IsSupportedImage(photo))
        {
            logger.LogWarning("Rejected book recognition upload request for family {FamilyId} because the photo content type {ContentType} is unsupported.", current.FamilyId, photo.ContentType);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["photo"] = ["Recognition photo must be a supported image file."],
            });
        }

        if (photo.Length > ScanPhotoUploadPolicy.MaxUploadBytes)
        {
            logger.LogWarning("Rejected book recognition upload request for family {FamilyId} because the photo exceeded the maximum size.", current.FamilyId);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["photo"] = ["Recognition photo is too large."],
            });
        }

        string storedPhotoPath;
        try
        {
            logger.LogInformation(
                "Storing recognition upload for family {FamilyId} with file {FileName} ({ContentType}, {Length} bytes).",
                current.FamilyId,
                photo.FileName,
                photo.ContentType ?? "<unknown>",
                photo.Length);

            await using var source = photo.OpenReadStream();
            storedPhotoPath = await photoStorage.StoreTemporaryAsync(
                source,
                photo.FileName,
                photo.ContentType ?? string.Empty,
                cancellationToken);

            logger.LogInformation(
                "Stored recognition upload for family {FamilyId} at {StoredPhotoPath}.",
                current.FamilyId,
                storedPhotoPath);
        }
        catch (ArgumentException exception)
        {
            logger.LogWarning(exception, "Failed to store temporary photo for family {FamilyId}.", current.FamilyId);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["photo"] = [exception.Message],
            });
        }

        try
        {
            var dto = await recognitionJobService.CreateAsync(
                current.FamilyId,
                storedPhotoPath,
                ToLanguageCode(current.PreferredLanguage),
                cancellationToken);

            logger.LogInformation(
                "Book recognition upload request for family {FamilyId} created job {JobId}.",
                current.FamilyId,
                dto.JobId);

            logger.LogInformation(
                "Book recognition upload request for family {FamilyId} completed successfully with status {StatusCode}.",
                current.FamilyId,
                StatusCodes.Status202Accepted);

            return Results.Accepted(
                $"/api/book-recognition-jobs/{dto.JobId}",
                ToResponse(dto));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to create book recognition job for family {FamilyId}.", current.FamilyId);
            await photoStorage.DeleteAsync(storedPhotoPath, cancellationToken);

            logger.LogWarning(
                "Book recognition upload request for family {FamilyId} failed before returning a response.",
                current.FamilyId);

            return exception switch
            {
                KeyNotFoundException => Results.NotFound(),
                ArgumentOutOfRangeException or ArgumentException => Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["photo"] = [exception.Message],
                }),
                _ => Results.Problem(
                    detail: "An unexpected error occurred while creating the recognition job.",
                    statusCode: StatusCodes.Status500InternalServerError),
            };
        }
    }

    private static async Task<IResult> GetBookRecognitionJobAsync(
        Guid jobId,
        ICurrentFamilyContextAccessor accessor,
        IBookRecognitionJobService recognitionJobService,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        var dto = await recognitionJobService.GetAsync(current.FamilyId, jobId, cancellationToken);
        if (dto is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(dto));
    }

    private static bool IsSupportedImage(IFormFile photo)
    {
        return photo.ContentType is not null && ScanPhotoUploadPolicy.AllowedImageContentTypes.Contains(photo.ContentType);
    }

    private static string ToLanguageCode(PreferredLanguage preferredLanguage)
    {
        return preferredLanguage switch
        {
            PreferredLanguage.Chinese => "zh",
            _ => "en",
        };
    }

    private static BookRecognitionJobResponse ToResponse(BookRecognitionJobDto dto)
    {
        var candidates = dto.Candidates
            .Select(candidate => new BookRecognitionCandidateResponse(
                candidate.CandidateId,
                candidate.DisplayTitle,
                candidate.EvidenceText,
                candidate.Rank,
                candidate.MetadataMatches
                    .Select(metadata => new BookMetadataCandidateResponse(
                        metadata.Source,
                        metadata.SourceId,
                        metadata.Title,
                        metadata.Subtitle,
                        metadata.Authors,
                        metadata.Publisher,
                        metadata.PublishedDate,
                        metadata.Language,
                        metadata.Description,
                        metadata.Isbn10,
                        metadata.Isbn13,
                        metadata.ThumbnailUrl,
                        metadata.InfoUrl))
                    .ToList()))
            .ToList();

        return new BookRecognitionJobResponse(
            dto.JobId,
            dto.FamilyId,
            dto.Status,
            dto.SourcePhotoPath,
            candidates,
            dto.Warnings,
            dto.FailureMessage,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}
