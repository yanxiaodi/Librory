using Librory.Api.Contracts;
using Librory.Api.Validation;
using Librory.Application.Families;
using Librory.Application.Intake;
using Librory.Application.Metadata;
using Librory.Domain.Models;

namespace Librory.Api.Endpoints;

internal static class ScanPurchaseEndpoints
{
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
    }

    private static BookMetadataCandidate ToMetadataCandidate(BookMetadataImportCandidateRequest request)
    {
        return new BookMetadataCandidate(
            request.Source,
            request.SourceId,
            request.Title,
            request.Subtitle,
            request.Authors ?? [],
            request.Publisher,
            request.PublishedDate,
            request.Language,
            request.Description,
            request.Isbn10,
            request.Isbn13,
            request.ThumbnailUrl,
            request.InfoUrl);
    }
}
