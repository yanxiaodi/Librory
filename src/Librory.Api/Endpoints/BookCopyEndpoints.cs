using Librory.Api.Contracts;
using Librory.Api.Validation;
using Librory.Application.Families;
using Librory.Application.Intake;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Api.Endpoints;

internal static class BookCopyEndpoints
{
    public static IEndpointRouteBuilder MapBookCopyEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/family/current/book-copies")
            .RequireAuthorization()
            .WithTags("Books");

        group.MapPost(string.Empty, CreateBookCopyAsync)
            .WithName("CreateBookCopy")
            .WithSummary("Create a book copy.")
            .WithDescription("Creates a copy for the current family from a resolved edition and returns duplicate detection summary data.")
            .Produces<ManualBookIntakeResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("{bookCopyId:guid}", GetBookCopyAsync)
            .WithName("GetBookCopy")
            .WithSummary("Get a book copy by id.")
            .WithDescription("Returns a single book copy for the current family.")
            .Produces<BookCopyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateBookCopyAsync(
        CreateBookCopyRequest request,
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        if (ApiValidation.Required(
                new ValidationField("bookEditionId", request.BookEditionId?.ToString(), "Book edition id is required."))
            is IResult validationProblem)
        {
            return validationProblem;
        }

        if (request.BookEditionId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bookEditionId"] = ["Book edition id must not be empty."],
            });
        }

        var bookEditionId = request.BookEditionId.GetValueOrDefault();

        var family = await LoadFamilyForDuplicateDetectionAsync(db, current.FamilyId, cancellationToken);
        if (family is null)
        {
            return Results.NotFound();
        }

        var member = family.Members.SingleOrDefault(x => x.Id == current.MemberId);
        if (member is null)
        {
            return Results.Unauthorized();
        }

        var edition = await LoadBookEditionAsync(db, bookEditionId, cancellationToken);
        if (edition is null)
        {
            return Results.NotFound();
        }

        ManualBookIntakeResult intakeResult;
        try
        {
            intakeResult = ManualBookIntakeRecorder.RecordWithDuplicateDetection(
                family,
                new ManualBookIntakeRequest(
                    edition,
                    member,
                    request.DuplicateStatus,
                    request.Condition,
                    request.PurchaseStore,
                    request.PurchasePrice,
                    request.ShelfLocation,
                    request.PurchasedAt,
                    request.IntakeNotes));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException or ArgumentException)
        {
            return Results.Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        await db.SaveChangesAsync(cancellationToken);

        var response = ManualBookIntakeResponseFactory.Create(intakeResult);
        return Results.CreatedAtRoute(
            "GetBookCopy",
            new { bookCopyId = response.Copy.BookCopyId },
            response);
    }

    private static async Task<IResult> GetBookCopyAsync(
        Guid bookCopyId,
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        var copy = await db.BookCopies
            .SingleOrDefaultAsync(x => x.Id == bookCopyId && x.FamilyId == current.FamilyId, cancellationToken);

        return copy is null
            ? Results.NotFound()
            : Results.Ok(BookCopyResponseFactory.Create(copy));
    }

    private static Task<Family?> LoadFamilyForDuplicateDetectionAsync(
        LibroryDbContext db,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return db.Families
            .Include(x => x.Members)
            .Include(x => x.BookCopies)
                .ThenInclude(x => x.BookEdition)
                    .ThenInclude(x => x.BookWork)
            .SingleOrDefaultAsync(x => x.Id == familyId, cancellationToken);
    }

    private static Task<BookEdition?> LoadBookEditionAsync(
        LibroryDbContext db,
        Guid bookEditionId,
        CancellationToken cancellationToken)
    {
        return db.BookEditions
            .Include(x => x.BookWork)
            .SingleOrDefaultAsync(x => x.Id == bookEditionId, cancellationToken);
    }
}
