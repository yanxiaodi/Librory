using Librory.Api.Contracts;
using Librory.Application.Families;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Api.Endpoints;

internal static class FamilyEndpoints
{
    public static IEndpointRouteBuilder MapFamilyEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api")
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentFamilyAsync)
            .WithName("GetCurrentFamily")
            .Produces<CurrentFamilyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/family/current", GetCurrentFamilyAsync)
            .WithName("GetCurrentFamilySummary")
            .Produces<CurrentFamilyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetCurrentFamilyAsync(
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        var family = await db.Families
            .Include(x => x.Members)
            .Include(x => x.BookCopies)
            .Include(x => x.WishlistItems)
            .SingleOrDefaultAsync(x => x.Id == current.FamilyId, cancellationToken);

        if (family is null)
        {
            return Results.NotFound();
        }

        var member = family.Members.SingleOrDefault(x => x.Id == current.MemberId);
        if (member is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new CurrentFamilyResponse(
            family.Id,
            family.Name,
            member.Id,
            member.DisplayName,
            member.Role,
            member.PreferredLanguage,
            family.Members.Count,
            family.BookCopies.Count,
            family.WishlistItems.Count));
    }
}
