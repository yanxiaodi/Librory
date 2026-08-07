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

        group.MapGet(string.Empty, GetCurrentRecommendationProfileAsync)
            .WithName("GetRecommendationProfile")
            .WithSummary("Get the current member's recommendation profile.")
            .Produces<RecommendationProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
        group.MapPut(string.Empty, UpsertCurrentRecommendationProfileAsync)
            .WithName("UpsertRecommendationProfile")
            .WithSummary("Create or update the current member's recommendation profile.")
            .Produces<RecommendationProfileResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        var memberGroup = app.MapGroup("/api/family/current/members/{memberId:guid}/recommendation-profile")
            .RequireAuthorization()
            .WithTags("Recommendations");
        memberGroup.MapGet(string.Empty, GetMemberRecommendationProfileAsync)
            .WithName("GetMemberRecommendationProfile")
            .Produces<RecommendationProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
        memberGroup.MapPut(string.Empty, UpsertMemberRecommendationProfileAsync)
            .WithName("UpsertMemberRecommendationProfile")
            .Produces<RecommendationProfileResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetCurrentRecommendationProfileAsync(
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken ct)
    {
        var current = accessor.Current;
        return current is null
            ? Results.Unauthorized()
            : await GetMemberProfileAsync(current.MemberId, db, current, ct);
    }

    private static async Task<IResult> GetMemberRecommendationProfileAsync(
        Guid memberId,
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken ct)
    {
        var current = accessor.Current;
        return current is null
            ? Results.Unauthorized()
            : await GetMemberProfileAsync(memberId, db, current, ct);
    }

    private static async Task<IResult> GetMemberProfileAsync(
        Guid memberId,
        LibroryDbContext db,
        CurrentFamilyContext current,
        CancellationToken ct)
    {
        var member = await LoadActiveMemberAsync(db, current.FamilyId, memberId, ct);
        if (member is null) return Results.NotFound();

        var profile = await LoadProfileAsync(db, current.FamilyId, memberId, ct);
        if (profile is null) return Results.NotFound();

        var isOwnerOrAdmin = await CanEditMemberAsync(db, current, memberId, ct);
        if (!isOwnerOrAdmin && profile.ProfileVisibility == ProfileVisibility.Private)
        {
            return Results.Forbid();
        }

        return Results.Ok(RecommendationProfileResponseFactory.Create(profile, isOwnerOrAdmin));
    }

    private static async Task<IResult> UpsertCurrentRecommendationProfileAsync(
        UpsertRecommendationProfileRequest request,
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken ct)
    {
        var current = accessor.Current;
        return current is null
            ? Results.Unauthorized()
            : await UpsertMemberProfileAsync(current.MemberId, request, db, current, ct);
    }

    private static async Task<IResult> UpsertMemberRecommendationProfileAsync(
        Guid memberId,
        UpsertRecommendationProfileRequest request,
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken ct)
    {
        var current = accessor.Current;
        return current is null
            ? Results.Unauthorized()
            : await UpsertMemberProfileAsync(memberId, request, db, current, ct);
    }

    private static async Task<IResult> UpsertMemberProfileAsync(
        Guid memberId,
        UpsertRecommendationProfileRequest request,
        LibroryDbContext db,
        CurrentFamilyContext current,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var member = await LoadActiveMemberAsync(db, current.FamilyId, memberId, ct);
        if (member is null) return Results.NotFound();
        if (!await CanEditMemberAsync(db, current, memberId, ct)) return Results.Forbid();

        var changes = request.ToChanges();
        var profile = await LoadProfileAsync(db, current.FamilyId, memberId, ct);

        try
        {
            if (profile is null)
            {
                profile = RecommendationProfile.Create(member);
                profile.ApplyChanges(changes);
                db.RecommendationProfiles.Add(profile);
            }
            else
            {
                profile.ApplyChanges(changes);
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException or ArgumentException)
        {
            return Results.Problem(detail: exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(RecommendationProfileResponseFactory.Create(profile, includePrivateNotes: true));
    }

    private static Task<Member?> LoadActiveMemberAsync(
        LibroryDbContext db,
        Guid familyId,
        Guid memberId,
        CancellationToken ct) =>
        db.Members.SingleOrDefaultAsync(x => x.Id == memberId && x.FamilyId == familyId && x.IsActive, ct);

    private static Task<RecommendationProfile?> LoadProfileAsync(
        LibroryDbContext db,
        Guid familyId,
        Guid memberId,
        CancellationToken ct) =>
        db.RecommendationProfiles
            .Include(x => x.Member)
            .SingleOrDefaultAsync(x => x.MemberId == memberId && x.Member.FamilyId == familyId, ct);

    private static async Task<bool> CanEditMemberAsync(
        LibroryDbContext db,
        CurrentFamilyContext current,
        Guid targetMemberId,
        CancellationToken ct)
    {
        if (current.MemberId == targetMemberId) return true;

        return await db.Members.AnyAsync(
            x => x.Id == current.MemberId &&
                 x.FamilyId == current.FamilyId &&
                 x.IsActive &&
                 x.Role == MemberRole.Admin,
            ct);
    }
}
