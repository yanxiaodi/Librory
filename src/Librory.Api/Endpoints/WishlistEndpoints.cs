using Librory.Api.Contracts;
using Librory.Application.Families;
using Librory.Application.Wishlist;
using Librory.Api.Validation;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Api.Endpoints;

internal static class WishlistEndpoints
{
    public static IEndpointRouteBuilder MapWishlistEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/family/current/wishlist")
            .RequireAuthorization()
            .WithTags("Wishlist");

        group.MapGet(string.Empty, GetWishlistAsync)
            .WithName("GetWishlist")
            .WithSummary("Get the current family's wishlist.")
            .WithDescription("Returns a paged newest-first wishlist view for the signed-in family.")
            .Produces<WishlistPageResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost(string.Empty, CreateWishlistItemAsync)
            .WithName("CreateWishlistItem")
            .WithSummary("Create a wishlist item.")
            .WithDescription("Adds a wishlist entry that can optionally link to a known work or edition.")
            .Produces<WishlistItemDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("{wishlistItemId:guid}", GetWishlistItemAsync)
            .WithName("GetWishlistItem")
            .WithSummary("Get a wishlist item by id.")
            .WithDescription("Returns a single wishlist item for the signed-in family.")
            .Produces<WishlistItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetWishlistAsync(
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

            if (page < 1)
            {
                errors["page"] = ["Page must be at least 1."];
            }

            if (pageSize < 1 || pageSize > 100)
            {
                errors["pageSize"] = ["Page size must be between 1 and 100."];
            }

            return Results.ValidationProblem(errors);
        }

        var query = db.WishlistItems
            .Where(x => x.FamilyId == current.FamilyId)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Results.Ok(new WishlistPageResponse(
            items.Select(WishlistItemDtoFactory.Create).ToList(),
            page,
            pageSize,
            totalCount));
    }

    private static async Task<IResult> CreateWishlistItemAsync(
        CreateWishlistItemRequest request,
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
                new ValidationField("title", request.Title, "Title is required."))
            is IResult validationProblem)
        {
            return validationProblem;
        }

        var family = await db.Families
            .SingleOrDefaultAsync(x => x.Id == current.FamilyId, cancellationToken);
        if (family is null)
        {
            return Results.NotFound();
        }

        var member = await db.Members
            .SingleOrDefaultAsync(
                x => x.Id == current.MemberId && x.FamilyId == current.FamilyId,
                cancellationToken);
        if (member is null)
        {
            return Results.Unauthorized();
        }

        BookWork? bookWork = null;
        if (request.BookWorkId.HasValue)
        {
            bookWork = await db.BookWorks
                .SingleOrDefaultAsync(x => x.Id == request.BookWorkId.Value, cancellationToken);
            if (bookWork is null)
            {
                return Results.NotFound();
            }
        }

        BookEdition? bookEdition = null;
        if (request.BookEditionId.HasValue)
        {
            bookEdition = await db.BookEditions
                .Include(x => x.BookWork)
                .SingleOrDefaultAsync(x => x.Id == request.BookEditionId.Value, cancellationToken);
            if (bookEdition is null)
            {
                return Results.NotFound();
            }
        }

        var recordRequest = new WishlistItemRequest(
            request.Title,
            request.Author,
            bookWork,
            bookEdition);

        WishlistItem wishlistItem;
        try
        {
            wishlistItem = WishlistRecorder.Record(family, recordRequest);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        db.WishlistItems.Add(wishlistItem);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/family/current/wishlist/{wishlistItem.Id}",
            WishlistItemDtoFactory.Create(wishlistItem));
    }

    private static async Task<IResult> GetWishlistItemAsync(
        Guid wishlistItemId,
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        var wishlistItem = await db.WishlistItems
            .SingleOrDefaultAsync(
                x => x.Id == wishlistItemId && x.FamilyId == current.FamilyId,
                cancellationToken);

        return wishlistItem is null
            ? Results.NotFound()
            : Results.Ok(WishlistItemDtoFactory.Create(wishlistItem));
    }
}
