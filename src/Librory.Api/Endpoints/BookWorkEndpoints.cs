using Librory.Api.Contracts;
using Librory.Api.Validation;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Api.Endpoints;

internal static class BookWorkEndpoints
{
    public static IEndpointRouteBuilder MapBookWorkEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/book-works")
            .RequireAuthorization()
            .WithTags("Books");

        group.MapPost(string.Empty, CreateBookWorkAsync)
            .WithName("CreateBookWork")
            .WithSummary("Create a book work.")
            .WithDescription("Creates a canonical work and optionally creates the first edition when edition details are provided.")
            .Produces<BookWorkResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("{bookWorkId:guid}", GetBookWorkAsync)
            .WithName("GetBookWork")
            .WithSummary("Get a book work by id.")
            .WithDescription("Returns the requested work together with its editions.")
            .Produces<BookWorkResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateBookWorkAsync(
        CreateBookWorkRequest request,
        LibroryDbContext db,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (ApiValidation.Required(
                new ValidationField("title", request.Title, "Title is required."))
            is IResult validationProblem)
        {
            return validationProblem;
        }

        var work = BookWork.Create(request.Title, request.Author);
        if (HasEditionDetails(request))
        {
            work.AddEdition(request.Isbn, request.Format, request.PublicationYear);
        }

        db.BookWorks.Add(work);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/book-works/{work.Id}",
            BookWorkResponseFactory.Create(work));
    }

    private static async Task<IResult> GetBookWorkAsync(
        Guid bookWorkId,
        LibroryDbContext db,
        CancellationToken cancellationToken)
    {
        var work = await db.BookWorks
            .Include(x => x.Editions)
            .SingleOrDefaultAsync(x => x.Id == bookWorkId, cancellationToken);

        return work is null
            ? Results.NotFound()
            : Results.Ok(BookWorkResponseFactory.Create(work));
    }

    private static bool HasEditionDetails(CreateBookWorkRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Isbn)
            || !string.IsNullOrWhiteSpace(request.Format)
            || request.PublicationYear.HasValue;
    }
}
