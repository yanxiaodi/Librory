using Librory.Api.Contracts;
using Librory.Application.Families;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Librory.Api.Endpoints;

internal static class FamilyEndpoints
{
    public static IEndpointRouteBuilder MapFamilyEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api")
            .RequireAuthorization()
            .WithTags("Family");

        group.MapGet("/family/current", GetCurrentFamilyAsync)
            .WithName("GetCurrentFamily")
            .WithSummary("Get the current family summary.")
            .WithDescription("Returns the active family, current member, and simple family counts for the signed-in user.")
            .Produces<CurrentFamilyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/families", ListFamiliesAsync).WithName("ListFamilies");
        group.MapPost("/families", CreateFamilyAsync).WithName("CreateFamily");
        group.MapPost("/families/{familyId:guid}/select", SelectFamilyAsync).WithName("SelectFamily");
        group.MapGet("/family/current/members", ListMembersAsync).WithName("ListFamilyMembers");
        group.MapPost("/family/current/members", CreateMemberAsync).WithName("CreateFamilyMember");
        group.MapPatch("/family/current/members/{memberId:guid}", UpdateMemberAsync).WithName("UpdateFamilyMember");
        group.MapPost("/family/current/members/{memberId:guid}/deactivate", (Guid memberId, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct) => SetMemberActiveAsync(memberId, false, db, accessor, ct)).WithName("DeactivateFamilyMember");
        group.MapPost("/family/current/members/{memberId:guid}/reactivate", (Guid memberId, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct) => SetMemberActiveAsync(memberId, true, db, accessor, ct)).WithName("ReactivateFamilyMember");
        group.MapGet("/family/current/invitations", ListInvitationsAsync).WithName("ListFamilyInvitations");
        group.MapPost("/family/current/invitations", CreateInvitationAsync).WithName("CreateFamilyInvitation");
        group.MapPost("/family/current/invitations/{invitationId:guid}/resend", ResendInvitationAsync).WithName("ResendFamilyInvitation");
        group.MapPost("/family/current/invitations/{invitationId:guid}/revoke", RevokeInvitationAsync).WithName("RevokeFamilyInvitation");
        group.MapPost("/family/current/members/{memberId:guid}/invitation", (Guid memberId, CreateFamilyInvitationRequest request, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct) => CreateInvitationAsync(request with { TargetMemberId = memberId }, db, accessor, ct)).WithName("InviteFamilyMember");

        app.MapGet("/api/family-invitations/{token}", PreviewInvitationAsync).AllowAnonymous().WithName("PreviewFamilyInvitation");
        app.MapPost("/api/family-invitations/{token}/accept", AcceptInvitationAsync).RequireAuthorization().WithName("AcceptFamilyInvitation");

        return app;
    }

    private static async Task<IResult> ListFamiliesAsync(LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var accountId = await ResolveAccountIdAsync(db, current, ct);
        if (accountId is null) return Results.Unauthorized();
        var memberships = await db.Members.Include(x => x.Family).Where(x => x.UserAccountId == accountId && x.IsActive).ToListAsync(ct);
        return Results.Ok(memberships.Select(x => new FamilySummaryResponse(x.FamilyId, x.Family.Name, x.Id, x.DisplayName, x.Role, x.IsActive)));
    }

    private static async Task<IResult> CreateFamilyAsync(CreateFamilyRequest request, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var accountId = await ResolveAccountIdAsync(db, current, ct);
        if (accountId is null) return Results.Unauthorized();
        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.Id == accountId, ct);
        if (account is null) return Results.Unauthorized();
        var family = Family.CreateSharedFamily(request.FamilyName, request.MemberDisplayName, request.PreferredLanguage);
        family.Members.Single().LinkAccount(account);
        db.Families.Add(family);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/families/{family.Id}", new FamilySummaryResponse(family.Id, family.Name, family.Members.Single().Id, request.MemberDisplayName.Trim(), MemberRole.Admin, true));
    }

    private static async Task<IResult> SelectFamilyAsync(Guid familyId, HttpContext http, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var accountId = await ResolveAccountIdAsync(db, current, ct);
        if (accountId is null) return Results.Unauthorized();
        var member = await db.Members.SingleOrDefaultAsync(x => x.FamilyId == familyId && x.UserAccountId == accountId && x.IsActive, ct);
        if (member is null) return Results.NotFound();
        var family = await db.Families.SingleAsync(x => x.Id == familyId, ct);
        var identity = new System.Security.Claims.ClaimsIdentity(new System.Security.Claims.Claim[]
        {
            new(CurrentFamilyContextClaimTypes.AccountId, accountId!.Value.ToString()),
            new(CurrentFamilyContextClaimTypes.FamilyId, familyId.ToString()),
            new(CurrentFamilyContextClaimTypes.MemberId, member.Id.ToString()),
            new(CurrentFamilyContextClaimTypes.MemberRole, member.Role.ToString()),
            new(CurrentFamilyContextClaimTypes.PreferredLanguage, member.PreferredLanguage.ToString()),
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(identity));
        return Results.Ok(new FamilySummaryResponse(family.Id, family.Name, member.Id, member.DisplayName, member.Role, member.IsActive));
    }

    private static async Task<IResult> ListMembersAsync(LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var members = await db.Members.Where(x => x.FamilyId == current.FamilyId).OrderBy(x => x.DisplayName).ToListAsync(ct);
        return Results.Ok(members.Select(x => new FamilyMemberResponse(x.Id, x.DisplayName, x.Role, x.PreferredLanguage, x.IsActive, x.UserAccountId is not null)));
    }

    private static async Task<IResult> CreateMemberAsync(CreateMemberRequest request, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var authorization = await RequireActiveAdminAsync(db, current, ct);
        if (authorization is not null) return authorization;
        var family = await db.Families.Include(x => x.Members).SingleOrDefaultAsync(x => x.Id == current.FamilyId, ct);
        if (family is null) return Results.NotFound();
        var member = family.AddMember(request.DisplayName, MemberRole.Member, request.PreferredLanguage);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/family/current/members/{member.Id}", new FamilyMemberResponse(member.Id, member.DisplayName, member.Role, member.PreferredLanguage, member.IsActive, false));
    }

    private static async Task<IResult> UpdateMemberAsync(Guid memberId, UpdateMemberRequest request, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var authorization = await RequireActiveAdminAsync(db, current, ct);
        if (authorization is not null) return authorization;
        var member = await db.Members.SingleOrDefaultAsync(x => x.Id == memberId && x.FamilyId == current.FamilyId, ct);
        if (member is null) return Results.NotFound();
        if (request.DisplayName is not null) member.DisplayName = request.DisplayName.Trim();
        if (request.PreferredLanguage is not null) member.PreferredLanguage = request.PreferredLanguage.Value;
        if (request.Role is not null) member.Role = request.Role.Value;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new FamilyMemberResponse(member.Id, member.DisplayName, member.Role, member.PreferredLanguage, member.IsActive, member.UserAccountId is not null));
    }

    private static async Task<IResult> SetMemberActiveAsync(Guid memberId, bool active, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var authorization = await RequireActiveAdminAsync(db, current, ct);
        if (authorization is not null) return authorization;
        var member = await db.Members.SingleOrDefaultAsync(x => x.Id == memberId && x.FamilyId == current.FamilyId, ct);
        if (member is null) return Results.NotFound();
        if (active) member.Reactivate(); else member.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new FamilyMemberResponse(member.Id, member.DisplayName, member.Role, member.PreferredLanguage, member.IsActive, member.UserAccountId is not null));
    }

    private static async Task<Guid?> ResolveAccountIdAsync(LibroryDbContext db, CurrentFamilyContext current, CancellationToken ct)
    {
        if (current.AccountId != Guid.Empty) return current.AccountId;
        return await db.Members.Where(x => x.Id == current.MemberId).Select(x => x.UserAccountId).SingleOrDefaultAsync(ct);
    }

    private static async Task<IResult> ListInvitationsAsync(LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var authorization = await RequireActiveAdminAsync(db, current, ct);
        if (authorization is not null) return authorization;
        var invitations = await db.FamilyInvitations.Where(x => x.FamilyId == current.FamilyId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Results.Ok(invitations.Select(x => ToResponse(x)));
    }

    private static async Task<IResult> CreateInvitationAsync(CreateFamilyInvitationRequest request, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var authorization = await RequireActiveAdminAsync(db, current, ct);
        if (authorization is not null) return authorization;
        var email = NormalizeEmail(request.Email);
        if (email is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["Email is required."] });
        if (request.TargetMemberId is not null && !await db.Members.AnyAsync(x => x.Id == request.TargetMemberId && x.FamilyId == current.FamilyId && x.UserAccountId == null && x.IsActive, ct)) return Results.NotFound();
        var pending = await db.FamilyInvitations.Where(x => x.FamilyId == current.FamilyId && x.Email == email && x.Status == FamilyInvitationStatus.Pending).ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var invitation = FamilyInvitation.Create(current.FamilyId, email, HashToken(rawToken), current.MemberId, now.AddDays(7), request.TargetMemberId, now);
        foreach (var existing in pending) existing.Supersede(invitation.Id);
        db.FamilyInvitations.Add(invitation);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/family/current/invitations/{invitation.Id}", ToResponse(invitation, rawToken));
    }

    private static async Task<IResult> ResendInvitationAsync(Guid invitationId, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var authorization = await RequireActiveAdminAsync(db, current, ct);
        if (authorization is not null) return authorization;
        var old = await db.FamilyInvitations.SingleOrDefaultAsync(x => x.Id == invitationId && x.FamilyId == current.FamilyId, ct);
        if (old is null) return Results.NotFound();
        old.Expire(DateTimeOffset.UtcNow);
        if (old.Status != FamilyInvitationStatus.Pending) return Results.Conflict();
        var now = DateTimeOffset.UtcNow;
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var replacement = FamilyInvitation.Create(old.FamilyId, old.Email, HashToken(rawToken), current.MemberId, now.AddDays(7), old.TargetMemberId, now);
        old.Supersede(replacement.Id);
        db.FamilyInvitations.Add(replacement);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(replacement, rawToken));
    }

    private static async Task<IResult> RevokeInvitationAsync(Guid invitationId, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var authorization = await RequireActiveAdminAsync(db, current, ct);
        if (authorization is not null) return authorization;
        var invitation = await db.FamilyInvitations.SingleOrDefaultAsync(x => x.Id == invitationId && x.FamilyId == current.FamilyId, ct);
        if (invitation is null) return Results.NotFound();
        try { invitation.Revoke(current.MemberId, DateTimeOffset.UtcNow); } catch (InvalidOperationException) { return Results.Conflict(); }
        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(invitation));
    }

    private static async Task<IResult> PreviewInvitationAsync(string token, LibroryDbContext db, CancellationToken ct)
    {
        var invitation = await FindInvitationAsync(token, db, ct);
        if (invitation is null) return Results.NotFound();
        invitation.Expire(DateTimeOffset.UtcNow);
        if (invitation.Status != FamilyInvitationStatus.Pending) return Results.Conflict();
        var familyName = await db.Families.Where(x => x.Id == invitation.FamilyId).Select(x => x.Name).SingleAsync(ct);
        return Results.Ok(new { invitation.Id, familyName, invitation.Email, invitation.TargetMemberId, invitation.ExpiresAt });
    }

    private static async Task<IResult> AcceptInvitationAsync(string token, LibroryDbContext db, ICurrentFamilyContextAccessor accessor, CancellationToken ct)
    {
        var current = accessor.Current;
        if (current is null) return Results.Unauthorized();
        var accountId = await ResolveAccountIdAsync(db, current, ct);
        if (accountId is null) return Results.Unauthorized();
        var invitation = await FindInvitationAsync(token, db, ct);
        if (invitation is null) return Results.NotFound();
        invitation.Expire(DateTimeOffset.UtcNow);
        if (invitation.Status != FamilyInvitationStatus.Pending) return Results.Conflict();
        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.Id == accountId, ct);
        if (account is null || !string.Equals(account.Email, invitation.Email, StringComparison.OrdinalIgnoreCase)) return Results.Forbid();
        Member member;
        if (invitation.TargetMemberId is Guid targetId)
        {
            member = await db.Members.SingleAsync(x => x.Id == targetId && x.FamilyId == invitation.FamilyId && x.IsActive, ct);
            if (member.UserAccountId is not null && member.UserAccountId != accountId) return Results.Conflict();
        }
        else
        {
            var family = await db.Families.Include(x => x.Members).SingleAsync(x => x.Id == invitation.FamilyId, ct);
            member = family.AddMember(account.Email ?? "Family member", MemberRole.Member);
        }
        member.LinkAccount(account);
        invitation.Accept(accountId.Value, DateTimeOffset.UtcNow);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict();
        }
        return Results.Ok(new FamilySummaryResponse(member.FamilyId, await db.Families.Where(x => x.Id == member.FamilyId).Select(x => x.Name).SingleAsync(ct), member.Id, member.DisplayName, member.Role, member.IsActive));
    }

    private static async Task<IResult?> RequireActiveAdminAsync(
        LibroryDbContext db,
        CurrentFamilyContext? current,
        CancellationToken ct)
    {
        if (current is null) return Results.Unauthorized();

        var isActiveAdmin = await db.Members.AnyAsync(
            x => x.Id == current.MemberId &&
                 x.FamilyId == current.FamilyId &&
                 x.IsActive &&
                 x.Role == MemberRole.Admin,
            ct);

        return isActiveAdmin ? null : Results.Forbid();
    }

    private static Task<FamilyInvitation?> FindInvitationAsync(string token, LibroryDbContext db, CancellationToken ct) =>
        db.FamilyInvitations.SingleOrDefaultAsync(x => x.TokenHash == HashToken(token), ct);

    private static FamilyInvitationResponse ToResponse(FamilyInvitation x, string? rawToken = null) => new(
        x.Id,
        x.FamilyId,
        x.TargetMemberId,
        x.Email,
        x.Status,
        x.CreatedAt,
        x.ExpiresAt,
        rawToken is null ? null : $"/family-invitations/{rawToken}");
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
    private static string? NormalizeEmail(string? email) => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

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

        var bookCopyCount = await db.BookCopies.CountAsync(x => x.FamilyId == current.FamilyId, cancellationToken);
        var wishlistItemCount = await db.WishlistItems.CountAsync(x => x.FamilyId == current.FamilyId, cancellationToken);

        return Results.Ok(new CurrentFamilyResponse(
            family.Id,
            family.Name,
            member.Id,
            member.DisplayName,
            member.Role,
            member.PreferredLanguage,
            family.Members.Count,
            bookCopyCount,
            wishlistItemCount));
    }
}
