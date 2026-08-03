using Librory.Api.Contracts;
using Librory.Api.Validation;
using Librory.Application.Metadata;
using Librory.Domain.Models;
using System.Text.Json;

namespace Librory.Api.Endpoints;

internal static class BookMetadataEndpoints
{
    private const int DefaultMaxResults = 10;
    private const int MaxAllowedResults = 40;

    public static IEndpointRouteBuilder MapBookMetadataEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/book-metadata")
            .WithTags("Metadata");

        group.MapGet("search", SearchByTitleAsync)
            .AllowAnonymous()
            .WithName("SearchBookMetadata")
            .WithSummary("Search book metadata by title.")
            .WithDescription("Queries a book metadata provider for titles that match the supplied book title.")
            .Produces<BookMetadataSearchResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapPost("import", ImportAsync)
            .RequireAuthorization()
            .WithName("ImportBookMetadata")
            .WithSummary("Import canonical book metadata.")
            .WithDescription("Imports a normalized metadata candidate into the canonical catalog as a book work with an optional first edition.")
            .Produces<BookWorkResponse>(StatusCodes.Status201Created)
            .Produces<BookWorkResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> ImportAsync(
        BookMetadataImportRequest request,
        IBookMetadataImportService importService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Candidate);

        if (ApiValidation.Required(
                new ValidationField("candidate.source", request.Candidate.Source, "Source is required."),
                new ValidationField("candidate.sourceId", request.Candidate.SourceId, "Source id is required."),
                new ValidationField("candidate.title", request.Candidate.Title, "Title is required."))
            is IResult validationProblem)
        {
            return validationProblem;
        }

        var candidate = new BookMetadataCandidate(
            request.Candidate.Source.Trim(),
            request.Candidate.SourceId.Trim(),
            request.Candidate.Title.Trim(),
            TrimToNull(request.Candidate.Subtitle),
            NormalizeAuthors(request.Candidate.Authors),
            TrimToNull(request.Candidate.Publisher),
            TrimToNull(request.Candidate.PublishedDate),
            TrimToNull(request.Candidate.Language),
            TrimToNull(request.Candidate.Description),
            TrimToNull(request.Candidate.Isbn10),
            TrimToNull(request.Candidate.Isbn13),
            TrimToNull(request.Candidate.ThumbnailUrl),
            TrimToNull(request.Candidate.InfoUrl));

        var result = await importService.ImportAsync(candidate, cancellationToken);
        var payload = BookWorkResponseFactory.Create(result.Work);

        return result.CreatedNew
            ? Results.Created($"/api/book-works/{result.Work.Id}", payload)
            : Results.Ok(payload);
    }

    private static async Task<IResult> SearchByTitleAsync(
        string title,
        string? language,
        int? maxResults,
        IBookMetadataSearchService searchService,
        CancellationToken cancellationToken)
    {
        if (ApiValidation.Required(
                new ValidationField("title", title, "Title is required."))
            is IResult validationProblem)
        {
            return validationProblem;
        }

        var results = maxResults ?? DefaultMaxResults;
        if (results <= 0 || results > MaxAllowedResults)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["maxResults"] = [$"Max results must be between 1 and {MaxAllowedResults}."],
            });
        }

        try
        {
            var result = await searchService.SearchByTitleAsync(title.Trim(), language, results, cancellationToken);
            return Results.Ok(BookMetadataResponseFactory.Create(result));
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [exception.ParamName ?? "title"] = [exception.Message],
            });
        }
        catch (JsonException exception)
        {
            return Results.Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (HttpRequestException exception)
        {
            return Results.Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static IReadOnlyList<string> NormalizeAuthors(IReadOnlyList<string>? authors)
    {
        return (authors ?? [])
            .Where(author => !string.IsNullOrWhiteSpace(author))
            .Select(author => author.Trim())
            .ToList();
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
