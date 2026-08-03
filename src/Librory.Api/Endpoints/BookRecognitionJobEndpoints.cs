using Librory.Api.Contracts;
using Librory.Api.Validation;
using Librory.Application.Families;
using Librory.Application.Recognition;
using Librory.Application.Scanning;
using Librory.Domain.Models;

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
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        if (request.ContentLength is null || request.ContentLength == 0 || !request.HasFormContentType)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["photo"] = ["Recognition photo is required."],
            });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var photo = form.Files.GetFile("photo");
        if (photo is null || photo.Length <= 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["photo"] = ["Recognition photo is required."],
            });
        }

        if (!IsSupportedImage(photo))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["photo"] = ["Recognition photo must be a supported image file."],
            });
        }

        if (photo.Length > ScanPhotoUploadPolicy.MaxUploadBytes)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["photo"] = ["Recognition photo is too large."],
            });
        }

        string storedPhotoPath;
        try
        {
            await using var source = photo.OpenReadStream();
            storedPhotoPath = await photoStorage.StoreTemporaryAsync(
                source,
                photo.FileName,
                photo.ContentType ?? string.Empty,
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
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

            return Results.Accepted(
                $"/api/book-recognition-jobs/{dto.JobId}",
                ToResponse(dto));
        }
        catch (Exception exception)
        {
            await photoStorage.DeleteAsync(storedPhotoPath, cancellationToken);

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
