using Librory.Api.Contracts;
using Librory.Application.Families;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Api.Endpoints;

internal static class RecommendationProfileEndpoints
{
    public static IEndpointRouteBuilder MapRecommendationProfileEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/family/current/recommendation-profile")
            .RequireAuthorization()
            .WithTags("Recommendations");

        group.MapGet(string.Empty, GetRecommendationProfileAsync)
            .WithName("GetRecommendationProfile")
            .WithSummary("Get the current member's recommendation profile.")
            .WithDescription("Returns the signed-in member's recommendation preferences when a profile exists.")
            .Produces<RecommendationProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut(string.Empty, UpsertRecommendationProfileAsync)
            .WithName("UpsertRecommendationProfile")
            .WithSummary("Create or update the current member's recommendation profile.")
            .WithDescription("Creates the signed-in member's profile if needed and preserves existing preferences when fields are omitted.")
            .Produces<RecommendationProfileResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetRecommendationProfileAsync(
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        var profile = await LoadCurrentRecommendationProfileAsync(db, current.FamilyId, current.MemberId, cancellationToken);

        return profile is null
            ? Results.NotFound()
            : Results.Ok(RecommendationProfileResponseFactory.Create(profile));
    }

    private static async Task<IResult> UpsertRecommendationProfileAsync(
        UpsertRecommendationProfileRequest request,
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

        var profile = await LoadCurrentRecommendationProfileAsync(db, current.FamilyId, current.MemberId, cancellationToken);
        if (profile is null)
        {
            var member = await LoadCurrentMemberAsync(db, current.FamilyId, current.MemberId, cancellationToken);
            if (member is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                profile = RecommendationProfile.Create(
                    member,
                    request.MinimumAge,
                    request.MaximumAge,
                    request.FavoriteAuthors,
                    request.FavoriteGenres,
                    request.FavoriteStyles);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException or ArgumentException)
            {
                return Results.Problem(
                    detail: exception.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }

            db.RecommendationProfiles.Add(profile);
        }
        else
        {
            try
            {
                profile.UpdatePreferences(
                    request.MinimumAge,
                    request.MaximumAge,
                    request.FavoriteAuthors,
                    request.FavoriteGenres,
                    request.FavoriteStyles);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException or ArgumentException)
            {
                return Results.Problem(
                    detail: exception.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(RecommendationProfileResponseFactory.Create(profile));
    }

    private static Task<Member?> LoadCurrentMemberAsync(
        LibroryDbContext db,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        return db.Members
            .SingleOrDefaultAsync(x => x.Id == memberId && x.FamilyId == familyId, cancellationToken);
    }

    private static Task<RecommendationProfile?> LoadCurrentRecommendationProfileAsync(
        LibroryDbContext db,
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        return db.RecommendationProfiles
            .Include(x => x.Member)
            .SingleOrDefaultAsync(
                x => x.MemberId == memberId && x.Member.FamilyId == familyId,
                cancellationToken);
    }
}
