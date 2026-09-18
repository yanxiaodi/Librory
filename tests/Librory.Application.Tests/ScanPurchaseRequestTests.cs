using Librory.Application.Intake;
using Librory.Application.Metadata;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public sealed class ScanPurchaseRequestTests
{
    [Fact]
    public void Scan_purchase_request_keeps_owner_and_purchase_request_identity_separate()
    {
        var ownerId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var candidate = new BookMetadataCandidate(
            "GoogleBooks",
            "volume-1",
            "Charlotte's Web",
            null,
            ["E. B. White"],
            null,
            "1952",
            "en",
            null,
            null,
            "9780061124952",
            null,
            null);

        var request = new ScanPurchaseRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            requestId,
            ownerId,
            candidate,
            DuplicateResolution.NewWork,
            BookCopyDuplicateStatus.ConfirmedUnique);

        Assert.Equal(ownerId, request.OwnerMemberId);
        Assert.Equal(requestId, request.PurchaseRequestId);
        Assert.Equal(DuplicateResolution.NewWork, request.DuplicateResolution);
        Assert.Equal(BookCopyDuplicateStatus.ConfirmedUnique, request.DuplicateStatus);
    }
}
