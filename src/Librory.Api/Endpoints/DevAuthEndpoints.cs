using System.Security.Claims;
using Librory.Api.Contracts;
using Librory.Application.Families;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Librory.Api.Validation;
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
            .WithTags("Development")
            .WithName("DevAuthLogin")
            .Produces<DevLoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        app.MapPost("/dev/auth/logout", async (HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            _ = cancellationToken;
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        })
            .AllowAnonymous()
            .WithTags("Development")
            .WithName("DevAuthLogout");

        app.MapPost("/dev/bootstrap", async (
            LibroryDbContext db,
            HttpContext httpContext,
            CancellationToken cancellationToken) => await BootstrapAsync(db, httpContext, cancellationToken))
            .AllowAnonymous()
            .WithTags("Development")
            .WithName("DevBootstrap")
            .Produces<DevLoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        return app;
    }

    private static Task<IResult> BootstrapAsync(
        LibroryDbContext db,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return LoginAsync(
            new DevLoginRequest("Demo Family", "Demo Admin", PreferredLanguage.English),
            db,
            httpContext,
            cancellationToken);
    }

    private static async Task<IResult> LoginAsync(
        DevLoginRequest request,
        LibroryDbContext db,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (ApiValidation.Required(
                new ValidationField("familyName", request.FamilyName, "Family name is required."),
                new ValidationField("memberDisplayName", request.MemberDisplayName, "Member display name is required."))
            is IResult validationProblem)
        {
            return validationProblem;
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
