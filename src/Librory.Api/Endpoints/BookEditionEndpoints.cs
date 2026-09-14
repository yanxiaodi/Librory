using Librory.Api.Contracts;
using Librory.Application.Families;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Api.Endpoints;

internal static class BookEditionEndpoints
{
    public static IEndpointRouteBuilder MapBookEditionEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut(
                "/api/family/current/book-editions/{bookEditionId:guid}/version",
                UpdateVersionAsync)
            .RequireAuthorization()
            .WithTags("Books")
            .WithName("UpdateBookEditionVersion")
            .WithSummary("Confirm the version metadata for a book edition.")
            .Produces<BookEditionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> UpdateVersionAsync(
        Guid bookEditionId,
        UpdateBookEditionVersionRequest? request,
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["Version update request is required."],
            });
        }

        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        var edition = await db.BookEditions
            .SingleOrDefaultAsync(edition => edition.Id == bookEditionId, cancellationToken);
        if (edition is null)
        {
            return Results.NotFound();
        }

        var belongsToFamily = await db.BookCopies
            .AnyAsync(copy => copy.FamilyId == current.FamilyId && copy.BookEditionId == bookEditionId, cancellationToken);
        if (!belongsToFamily)
        {
            return Results.NotFound();
        }

        try
        {
            edition.UpdateVersion(request.Isbn, request.Format, request.PublicationYear);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return Results.Problem(detail: exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new BookEditionResponse(
            edition.Id,
            edition.Isbn,
            edition.Format,
            edition.PublicationYear,
            edition.IsProvisional));
    }
}
