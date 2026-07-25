using Librory.Api.Contracts;
using Librory.Api.Validation;
using Librory.Application.Families;
using Librory.Application.Scanning;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Api.Endpoints;

internal static class ScanSessionEndpoints
{
    public static IEndpointRouteBuilder MapScanSessionEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/family/current/scan-sessions")
            .RequireAuthorization()
            .WithTags("Scanning");

        group.MapPost(string.Empty, CreateScanSessionAsync)
            .WithName("CreateScanSession")
            .WithSummary("Create a scan session.")
            .WithDescription("Creates a temporary shelf scan session and stores any initial recognized candidates.")
            .Produces<ScanSessionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("{scanSessionId:guid}", GetScanSessionAsync)
            .WithName("GetScanSession")
            .WithSummary("Get a scan session by id.")
            .WithDescription("Returns the stored scan session and candidates for the current family.")
            .Produces<ScanSessionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateScanSessionAsync(
        CreateScanSessionRequest request,
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
                new ValidationField("shelfPhotoPath", request.ShelfPhotoPath, "Shelf photo path is required."))
            is IResult validationProblem)
        {
            return validationProblem;
        }

        if (request.RetentionWindowDays is <= 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["retentionWindowDays"] = ["Retention window days must be positive."],
            });
        }

        var family = await LoadFamilyForScanAsync(db, current.FamilyId, cancellationToken);
        if (family is null)
        {
            return Results.NotFound();
        }

        var scanRequest = new ScanShelfRequest(
            current.FamilyId,
            current.PreferredLanguage.ToString(),
            request.ShelfPhotoPath,
            request.RetentionWindowDays.HasValue
                ? TimeSpan.FromDays(request.RetentionWindowDays.Value)
                : null,
            request.Candidates?.Select(candidate => new ScanCandidateInput(
                    candidate.DisplayTitle,
                    candidate.ConfidenceLabel,
                    candidate.Author,
                    candidate.RecommendationScore,
                    candidate.IsAlreadyOwned,
                    candidate.DuplicateMessage))
                .ToList());

        ScanSession session;
        try
        {
            session = ScanSessionRecorder.Record(family, scanRequest);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
        {
            return Results.Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        db.ScanSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/family/current/scan-sessions/{session.Id}",
            ToResponse(family, session));
    }

    private static async Task<IResult> GetScanSessionAsync(
        Guid scanSessionId,
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        var family = await LoadFamilyForScanAsync(db, current.FamilyId, cancellationToken);
        if (family is null)
        {
            return Results.NotFound();
        }

        var session = family.ScanSessions.SingleOrDefault(x => x.Id == scanSessionId);
        if (session is null || session.IsExpired())
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(family, session));
    }

    private static Task<Family?> LoadFamilyForScanAsync(
        LibroryDbContext db,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return db.Families
            .Include(x => x.BookCopies)
                .ThenInclude(x => x.BookEdition)
                    .ThenInclude(x => x.BookWork)
            .Include(x => x.ScanSessions)
                .ThenInclude(x => x.Candidates)
            .SingleOrDefaultAsync(x => x.Id == familyId, cancellationToken);
    }

    private static ScanSessionResponse ToResponse(Family family, ScanSession session)
    {
        var dto = ScanSessionDtoFactory.Create(family, session);
        var candidates = dto.Candidates
            .Select(candidate => new ScanCandidateResponse(
                candidate.Id,
                candidate.DisplayTitle,
                candidate.Author,
                candidate.RecommendationScore,
                candidate.IsAlreadyOwned,
                candidate.DuplicateMessage,
                candidate.ConfidenceLabel))
            .ToList();

        return new ScanSessionResponse(
            dto.ScanSessionId,
            dto.FamilyId,
            dto.ShelfPhotoPath,
            candidates,
            dto.ExpiresAt);
    }
}
