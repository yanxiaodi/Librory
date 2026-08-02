using Librory.Api.Contracts;
using Librory.Api.Validation;
using Librory.Application.Metadata;
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
            .AllowAnonymous()
            .WithTags("Metadata");

        group.MapGet("search", SearchByTitleAsync)
            .WithName("SearchBookMetadata")
            .WithSummary("Search book metadata by title.")
            .WithDescription("Queries a book metadata provider for titles that match the supplied book title.")
            .Produces<BookMetadataSearchResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        return app;
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
}
