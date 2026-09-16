using Librory.Api.Contracts;
using Librory.Api.Validation;
using Librory.Application.Families;
using Librory.Application.Intake;
using Librory.Application.Metadata;
using Librory.Domain.Models;

namespace Librory.Api.Endpoints;

internal static class ScanPurchaseEndpoints
{
    private const int MaxManualTitleLength = 300;
    private const int MaxManualAuthorLength = 300;
    private const int MaxIsbnLength = 32;
    private const int MaxFormatLength = 64;
    private const int MaxConditionLength = 200;
    private const int MaxPurchaseStoreLength = 200;
    private const int MaxShelfLocationLength = 200;
    private const int MaxIntakeNotesLength = 4000;
    private static readonly TimeSpan MinimumPurchaseAge = TimeSpan.FromDays(365 * 200);
    private static readonly TimeSpan MaximumFuturePurchaseSkew = TimeSpan.FromMinutes(5);

    public static IEndpointRouteBuilder MapScanPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(
                "/api/family/current/scan-sessions/{scanSessionId:guid}/candidates/{candidateId:guid}/purchase",
                PurchaseAsync)
            .RequireAuthorization()
            .WithTags("Scanning")
            .WithName("PurchaseScanCandidate")
            .WithSummary("Purchase a scan candidate into the family library.")
            .Produces<ScanPurchaseResponse>(StatusCodes.Status201Created)
            .Produces<DuplicateConfirmationResponse>(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> PurchaseAsync(
        Guid scanSessionId,
        Guid candidateId,
        ConfirmScanPurchaseRequest? request,
        ICurrentFamilyContextAccessor accessor,
        IScanPurchaseService purchaseService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["Purchase request is required."],
            });
        }

        if (ApiValidation.Required(
                new ValidationField("purchaseRequestId", request.PurchaseRequestId.ToString(), "Purchase request id is required."),
                new ValidationField("ownerMemberId", request.OwnerMemberId.ToString(), "Owner member id is required."))
            is IResult validationProblem)
        {
            return validationProblem;
        }

        if (request.SelectedMetadata is not null)
        {
            var metadataErrors = MetadataCandidateValidation.Validate(request.SelectedMetadata, "selectedMetadata");
            if (metadataErrors.Count > 0)
            {
                return Results.ValidationProblem(metadataErrors);
            }
        }

        var purchaseErrors = ValidatePurchaseFields(request);
        if (purchaseErrors.Count > 0)
        {
            return Results.ValidationProblem(purchaseErrors);
        }

        var current = accessor.Current;
        if (current is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var result = await purchaseService.PurchaseAsync(
                new ScanPurchaseRequest(
                    scanSessionId,
                    candidateId,
                    request.PurchaseRequestId,
                    request.OwnerMemberId,
                    request.SelectedMetadata is null ? null : ToMetadataCandidate(request.SelectedMetadata),
                    request.DuplicateResolution,
                    request.DuplicateStatus,
                    request.ExistingBookEditionId,
                    request.ExistingBookWorkId,
                    request.ManualTitle,
                    request.ManualAuthor,
                    request.Isbn,
                    request.Format,
                    request.PublicationYear,
                    request.Condition,
                    request.PurchaseStore,
                    request.PurchasePrice,
                    request.ShelfLocation,
                    request.PurchasedAt,
                    request.IntakeNotes),
                cancellationToken);

            var response = new ScanPurchaseResponse(
                BookCopyResponseFactory.Create(result.Copy),
                BookWorkResponseFactory.Create(result.Work),
                result.Edition.Id,
                result.Edition.IsProvisional,
                result.Copy.DuplicateStatus,
                result.IsReplay);

            return Results.CreatedAtRoute(
                "GetBookCopy",
                new { bookCopyId = result.Copy.Id },
                response);
        }
        catch (DuplicateConfirmationRequiredException exception)
        {
            return Results.Conflict(new DuplicateConfirmationResponse(
                exception.Message,
                exception.DuplicateDetection.FollowUpHint,
                exception.DuplicateDetection.Matches
                    .Select(match => new DuplicateMatchResponse(
                        match.BookCopyId,
                        match.BookEditionId,
                        match.BookWorkId,
                        match.Title,
                        match.Isbn,
                        match.Format,
                        match.PublicationYear))
                    .ToList()));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [exception.ParamName ?? "request"] = [exception.Message],
            });
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(detail: exception.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ScanPurchaseRetryableException exception)
        {
            return Results.Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?>
                {
                    ["retryable"] = true,
                });
        }
    }

    private static BookMetadataCandidate ToMetadataCandidate(BookMetadataImportCandidateRequest request)
    {
        return new BookMetadataCandidate(
            request.Source.Trim(),
            request.SourceId.Trim(),
            request.Title.Trim(),
            TrimToNull(request.Subtitle),
            (request.Authors ?? []).Select(author => author.Trim()).ToArray(),
            TrimToNull(request.Publisher),
            TrimToNull(request.PublishedDate),
            TrimToNull(request.Language),
            TrimToNull(request.Description),
            TrimToNull(request.Isbn10),
            TrimToNull(request.Isbn13),
            TrimToNull(request.ThumbnailUrl),
            TrimToNull(request.InfoUrl));
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Dictionary<string, string[]> ValidatePurchaseFields(ConfirmScanPurchaseRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddMaxLength(errors, "manualTitle", request.ManualTitle, MaxManualTitleLength);
        AddMaxLength(errors, "manualAuthor", request.ManualAuthor, MaxManualAuthorLength);
        AddMaxLength(errors, "isbn", request.Isbn, MaxIsbnLength);
        AddMaxLength(errors, "format", request.Format, MaxFormatLength);
        AddMaxLength(errors, "condition", request.Condition, MaxConditionLength);
        AddMaxLength(errors, "purchaseStore", request.PurchaseStore, MaxPurchaseStoreLength);
        AddMaxLength(errors, "shelfLocation", request.ShelfLocation, MaxShelfLocationLength);
        AddMaxLength(errors, "intakeNotes", request.IntakeNotes, MaxIntakeNotesLength);

        if (request.PublicationYear is < 1000 or > 9999)
        {
            errors["publicationYear"] = ["Publication year must be between 1000 and 9999."];
        }

        if (request.PurchasePrice is < 0m or > 9999999999999999.99m
            || request.PurchasePrice.HasValue && request.PurchasePrice.Value != decimal.Round(request.PurchasePrice.Value, 2))
        {
            errors["purchasePrice"] = ["Purchase price must be non-negative and have at most two decimal places."];
        }

        var now = DateTimeOffset.UtcNow;
        if (request.PurchasedAt is { } purchasedAt
            && (purchasedAt < now.Subtract(MinimumPurchaseAge)
                || purchasedAt > now.Add(MaximumFuturePurchaseSkew)))
        {
            errors["purchasedAt"] = ["Purchase time must be within the last 200 years and not more than five minutes in the future."];
        }

        return errors;
    }

    private static void AddMaxLength(
        IDictionary<string, string[]> errors,
        string key,
        string? value,
        int maxLength)
    {
        if (value is not null && value.Trim().Length > maxLength)
        {
            errors[key] = [$"Value must be {maxLength} characters or fewer."];
        }
    }
}
