namespace Librory.Application.Intake;

public interface IScanPurchaseService
{
    Task<ScanPurchaseResult> PurchaseAsync(
        ScanPurchaseRequest request,
        CancellationToken cancellationToken);
}
