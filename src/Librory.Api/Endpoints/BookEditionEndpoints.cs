using Librory.Api.Contracts;
using Librory.Application.Families;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Api.Endpoints;

internal static class BookEditionEndpoints
{
    private const int MaxIsbnLength = 32;
    private const int MaxFormatLength = 64;

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

        var activeMember = await db.Members.AnyAsync(
            member => member.Id == current.MemberId
                      && member.FamilyId == current.FamilyId
                      && member.IsActive,
            cancellationToken);
        if (!activeMember)
        {
            return Results.Unauthorized();
        }

        var isbn = TrimToNull(request.Isbn);
        var format = TrimToNull(request.Format);
        var validationErrors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (isbn is not null && isbn.Length > MaxIsbnLength)
        {
            validationErrors["isbn"] = [$"ISBN must be {MaxIsbnLength} characters or fewer."];
        }

        if (format is not null && format.Length > MaxFormatLength)
        {
            validationErrors["format"] = [$"Format must be {MaxFormatLength} characters or fewer."];
        }

        if (request.PublicationYear is < 1000 or > 9999)
        {
            validationErrors["publicationYear"] = ["Publication year must be between 1000 and 9999."];
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var edition = await db.BookEditions
            .SingleOrDefaultAsync(edition => edition.Id == bookEditionId, cancellationToken);
        if (edition is null)
        {
            return Results.NotFound();
        }

        if (!edition.IsProvisional)
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
            edition.UpdateVersion(isbn, format, request.PublicationYear);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return Results.Problem(detail: exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                detail: "The version metadata could not be saved.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new BookEditionResponse(
            edition.Id,
            edition.Isbn,
            edition.Format,
            edition.PublicationYear,
            edition.IsProvisional));
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
