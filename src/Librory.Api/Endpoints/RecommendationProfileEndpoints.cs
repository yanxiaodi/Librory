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

        var member = await LoadCurrentMemberAsync(db, current.FamilyId, current.MemberId, cancellationToken);
        if (member is null)
        {
            return Results.Unauthorized();
        }

        var profile = await db.RecommendationProfiles
            .SingleOrDefaultAsync(x => x.MemberId == member.Id, cancellationToken);

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

        var member = await LoadCurrentMemberAsync(db, current.FamilyId, current.MemberId, cancellationToken);
        if (member is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var profile = await db.RecommendationProfiles
                .SingleOrDefaultAsync(x => x.MemberId == member.Id, cancellationToken);

            if (profile is null)
            {
                profile = RecommendationProfile.Create(
                    member,
                    request.MinimumAge,
                    request.MaximumAge,
                    request.FavoriteAuthors,
                    request.FavoriteGenres,
                    request.FavoriteStyles);

                db.RecommendationProfiles.Add(profile);
            }
            else
            {
                profile.UpdatePreferences(
                    request.MinimumAge,
                    request.MaximumAge,
                    request.FavoriteAuthors,
                    request.FavoriteGenres,
                    request.FavoriteStyles);
            }

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(RecommendationProfileResponseFactory.Create(profile));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException or ArgumentException)
        {
            return Results.Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
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
}
