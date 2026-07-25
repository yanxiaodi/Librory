using System.Security.Claims;
using Librory.Api.Contracts;
using Librory.Application.Families;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Librory.Api.Endpoints;

internal static class DevAuthEndpoints
{
    public static IEndpointRouteBuilder MapDevAuthEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/dev/auth/login", LoginAsync)
            .AllowAnonymous()
            .WithName("DevAuthLogin")
            .Produces<DevLoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> LoginAsync(
        DevLoginRequest request,
        LibroryDbContext db,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FamilyName) || string.IsNullOrWhiteSpace(request.MemberDisplayName))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["familyName"] = ["Family name is required."],
                ["memberDisplayName"] = ["Member display name is required."],
            });
        }

        var normalizedFamilyName = request.FamilyName.Trim();
        var normalizedMemberDisplayName = request.MemberDisplayName.Trim();

        var family = await db.Families
            .Include(x => x.Members)
            .SingleOrDefaultAsync(x => x.Name == normalizedFamilyName, cancellationToken);

        if (family is null)
        {
            family = Family.Create(normalizedFamilyName);
            var createdMember = family.AddMember(normalizedMemberDisplayName, MemberRole.Admin, request.PreferredLanguage);

            db.Families.Add(family);
            await db.SaveChangesAsync(cancellationToken);

            return await SignInAsync(httpContext, family, createdMember);
        }

        var existingMember = family.Members.SingleOrDefault(x => x.DisplayName == normalizedMemberDisplayName);
        if (existingMember is null)
        {
            existingMember = family.AddMember(normalizedMemberDisplayName, MemberRole.Admin, request.PreferredLanguage);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await SignInAsync(httpContext, family, existingMember);
    }

    private static async Task<IResult> SignInAsync(
        HttpContext httpContext,
        Family family,
        Member member)
    {
        var claims = new List<Claim>
        {
            new(CurrentFamilyContextClaimTypes.FamilyId, family.Id.ToString()),
            new(CurrentFamilyContextClaimTypes.MemberId, member.Id.ToString()),
            new(CurrentFamilyContextClaimTypes.MemberRole, member.Role.ToString()),
            new(CurrentFamilyContextClaimTypes.PreferredLanguage, member.PreferredLanguage.ToString()),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
            });

        return Results.Ok(new DevLoginResponse(
            family.Id,
            family.Name,
            member.Id,
            member.DisplayName,
            member.Role,
            member.PreferredLanguage));
    }
}
