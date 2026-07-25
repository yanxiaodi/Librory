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

        var family = Family.Create(request.FamilyName);
        var member = family.AddMember(request.MemberDisplayName, MemberRole.Admin, request.PreferredLanguage);

        db.Families.Add(family);
        await db.SaveChangesAsync(cancellationToken);

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
