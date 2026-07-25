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
    private const int MaxShelfPhotoPathLength = 400;

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

        group.MapPut("{scanSessionId:guid}/candidates/{candidateId:guid}", CorrectScanCandidateAsync)
            .WithName("CorrectScanCandidate")
            .WithSummary("Correct a scan candidate.")
            .WithDescription("Updates a single scan candidate in place without disturbing the rest of the session.")
            .Produces<ScanSessionResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("{scanSessionId:guid}/candidates/{candidateId:guid}/resolve", ResolveScanCandidateAsync)
            .WithName("ResolveScanCandidate")
            .WithSummary("Promote a scan candidate.")
            .WithDescription("Promotes a scan candidate into canonical catalog data and removes it from the temporary session.")
            .Produces<BookWorkResponse>(StatusCodes.Status201Created)
            .Produces<BookWorkResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{scanSessionId:guid}/candidates/{candidateId:guid}", DiscardScanCandidateAsync)
            .WithName("DiscardScanCandidate")
            .WithSummary("Discard a scan candidate.")
            .WithDescription("Removes an unwanted scan candidate from the temporary session without promoting it.")
            .Produces(StatusCodes.Status204NoContent)
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
        ICurrentFamilyContextAccessor accessor,
        IScanSessionService scanSessionService,
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

        var shelfPhotoPath = request.ShelfPhotoPath.Trim();
        if (shelfPhotoPath.Length > MaxShelfPhotoPathLength)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["shelfPhotoPath"] = [$"Shelf photo path must be {MaxShelfPhotoPathLength} characters or fewer."],
            });
        }

        var retentionWindow = TryCreateRetentionWindow(request.RetentionWindowDays, out var retentionProblem);
        if (retentionProblem is not null)
        {
            return retentionProblem;
        }

        if (request.Candidates is not null && request.Candidates.Any(candidate => candidate is null))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["candidates"] = ["Candidate entries cannot be null."],
            });
        }

        try
        {
            var dto = await scanSessionService.StartShelfScanAsync(
                new ScanShelfRequest(
                    current.FamilyId,
                    ToLanguageCode(current.PreferredLanguage),
                    shelfPhotoPath,
                    retentionWindow,
                    request.Candidates?.Select(candidate => new ScanCandidateInput(
                            candidate.DisplayTitle,
                            candidate.ConfidenceLabel,
                            candidate.Author,
                            candidate.RecommendationScore,
                            candidate.IsAlreadyOwned,
                            candidate.DuplicateMessage))
                        .ToList()),
                cancellationToken);

            return Results.Created(
                $"/api/family/current/scan-sessions/{dto.ScanSessionId}",
                ToResponse(dto));
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException or ArgumentOutOfRangeException or ArgumentException)
        {
            return exception switch
            {
                UnauthorizedAccessException => Results.Unauthorized(),
                KeyNotFoundException => Results.NotFound(),
                _ => Results.Problem(
                    detail: exception.Message,
                    statusCode: StatusCodes.Status400BadRequest),
            };
        }
    }

    private static string ToLanguageCode(PreferredLanguage preferredLanguage)
    {
        return preferredLanguage switch
        {
            PreferredLanguage.Chinese => "zh",
            _ => "en",
        };
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

        var family = await LoadFamilyForDuplicateDetectionAsync(db, current.FamilyId, cancellationToken);
        if (family is null)
        {
            return Results.NotFound();
        }

        var session = await LoadScanSessionAsync(db, current.FamilyId, scanSessionId, cancellationToken);
        if (session is null || session.IsExpired())
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(family, session));
    }

    private static async Task<IResult> CorrectScanCandidateAsync(
        Guid scanSessionId,
        Guid candidateId,
        UpdateScanCandidateRequest request,
        IScanSessionService scanSessionService,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (ApiValidation.Required(
                new ValidationField("displayTitle", request.DisplayTitle, "Display title is required."),
                new ValidationField("confidenceLabel", request.ConfidenceLabel, "Confidence label is required."))
            is IResult validationProblem)
        {
            return validationProblem;
        }

        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var dto = await scanSessionService.ApplyCorrectionAsync(
                scanSessionId,
                candidateId,
                new CorrectionRequest(
                    request.DisplayTitle,
                    request.ConfidenceLabel,
                    request.Author,
                    request.RecommendationScore,
                    request.IsAlreadyOwned,
                    request.DuplicateMessage),
                cancellationToken);

            return Results.Ok(ToResponse(dto));
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException or ArgumentOutOfRangeException or ArgumentException)
        {
            return exception switch
            {
                UnauthorizedAccessException => Results.Unauthorized(),
                KeyNotFoundException => Results.NotFound(),
                _ => Results.Problem(
                    detail: exception.Message,
                    statusCode: StatusCodes.Status400BadRequest),
            };
        }
    }

    private static async Task<IResult> ResolveScanCandidateAsync(
        Guid scanSessionId,
        Guid candidateId,
        ResolveScanCandidateRequest request,
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (ApiValidation.Required(
                new ValidationField("title", request.Title, "Title is required."))
            is IResult validationProblem)
        {
            return validationProblem;
        }

        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var session = await LoadScanSessionAsync(db, current.FamilyId, scanSessionId, cancellationToken);
            if (session is null || session.IsExpired())
            {
                return Results.NotFound();
            }

            var candidate = session.Candidates.SingleOrDefault(existing => existing.Id == candidateId);
            if (candidate is null)
            {
                return Results.NotFound();
            }

            var work = await ResolveBookWorkAsync(db, request, cancellationToken);
            session.RemoveCandidate(candidateId);

            await db.SaveChangesAsync(cancellationToken);

            var response = BookWorkResponseFactory.Create(work);
            return request.BookWorkId is null
                ? Results.Created($"/api/book-works/{work.Id}", response)
                : Results.Ok(response);
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException or ArgumentOutOfRangeException or ArgumentException)
        {
            return exception switch
            {
                KeyNotFoundException => Results.NotFound(),
                _ => Results.Problem(
                    detail: exception.Message,
                    statusCode: StatusCodes.Status400BadRequest),
            };
        }
    }

    private static async Task<IResult> DiscardScanCandidateAsync(
        Guid scanSessionId,
        Guid candidateId,
        LibroryDbContext db,
        ICurrentFamilyContextAccessor accessor,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        var session = await LoadScanSessionAsync(db, current.FamilyId, scanSessionId, cancellationToken);
        if (session is null || session.IsExpired())
        {
            return Results.NotFound();
        }

        var candidate = session.Candidates.SingleOrDefault(existing => existing.Id == candidateId);
        if (candidate is null)
        {
            return Results.NotFound();
        }

        session.RemoveCandidate(candidateId);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static Task<ScanSession?> LoadScanSessionAsync(
        LibroryDbContext db,
        Guid familyId,
        Guid scanSessionId,
        CancellationToken cancellationToken)
    {
        return db.ScanSessions
            .Include(x => x.Candidates)
            .SingleOrDefaultAsync(x => x.FamilyId == familyId && x.Id == scanSessionId, cancellationToken);
    }

    private static Task<Family?> LoadFamilyForDuplicateDetectionAsync(
        LibroryDbContext db,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return db.Families
            .Include(x => x.BookCopies)
                .ThenInclude(x => x.BookEdition)
                    .ThenInclude(x => x.BookWork)
            .SingleOrDefaultAsync(x => x.Id == familyId, cancellationToken);
    }

    private static async Task<BookWork> ResolveBookWorkAsync(
        LibroryDbContext db,
        ResolveScanCandidateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.BookWorkId is Guid bookWorkId)
        {
            var work = await db.BookWorks
                .Include(x => x.Editions)
                .SingleOrDefaultAsync(x => x.Id == bookWorkId, cancellationToken);

            if (work is null)
            {
                throw new KeyNotFoundException("Book work not found.");
            }

            work.CanonicalTitle = request.Title;
            var normalizedAuthor = NormalizeOptional(request.Author);
            if (normalizedAuthor is not null)
            {
                work.CanonicalAuthor = normalizedAuthor;
            }

            if (HasEditionDetails(request) && FindMatchingEdition(work, request) is null)
            {
                work.AddEdition(request.Isbn, request.Format, request.PublicationYear);
            }

            return work;
        }

        var created = BookWork.Create(request.Title, request.Author);
        if (HasEditionDetails(request))
        {
            created.AddEdition(request.Isbn, request.Format, request.PublicationYear);
        }

        db.BookWorks.Add(created);
        return created;
    }

    private static bool HasEditionDetails(ResolveScanCandidateRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Isbn)
            || !string.IsNullOrWhiteSpace(request.Format)
            || request.PublicationYear.HasValue;
    }

    private static BookEdition? FindMatchingEdition(BookWork work, ResolveScanCandidateRequest request)
    {
        if (!HasEditionDetails(request))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Isbn))
        {
            var isbn = request.Isbn.Trim();
            var matchingIsbn = work.Editions.FirstOrDefault(edition => string.Equals(edition.Isbn, isbn, StringComparison.OrdinalIgnoreCase));
            if (matchingIsbn is not null)
            {
                return matchingIsbn;
            }

            if (string.IsNullOrWhiteSpace(request.Format) && !request.PublicationYear.HasValue)
            {
                return null;
            }
        }

        var normalizedFormat = request.Format?.Trim();
        return work.Editions.FirstOrDefault(edition =>
            string.Equals(edition.Format, normalizedFormat, StringComparison.OrdinalIgnoreCase)
            && edition.PublicationYear == request.PublicationYear);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static TimeSpan? TryCreateRetentionWindow(int? retentionWindowDays, out IResult? validationProblem)
    {
        validationProblem = null;

        if (retentionWindowDays is null)
        {
            return null;
        }

        if (retentionWindowDays <= 0)
        {
            validationProblem = Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["retentionWindowDays"] = ["Retention window days must be positive."],
            });
            return null;
        }

        try
        {
            return TimeSpan.FromDays(retentionWindowDays.Value);
        }
        catch (OverflowException)
        {
            validationProblem = Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["retentionWindowDays"] = ["Retention window days is too large."],
            });
            return null;
        }
    }

    private static ScanSessionResponse ToResponse(Family family, ScanSession session)
    {
        var dto = ScanSessionDtoFactory.Create(family, session);
        return ToResponse(dto);
    }

    private static ScanSessionResponse ToResponse(ScanSessionDto dto)
    {
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
