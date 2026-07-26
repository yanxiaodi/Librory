using Librory.Application.Intake;

namespace Librory.Api.Contracts;

public static class ManualBookIntakeResponseFactory
{
    public static ManualBookIntakeResponse Create(ManualBookIntakeResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ManualBookIntakeResponse(
            BookCopyResponseFactory.Create(result.Copy),
            result.HasPotentialDuplicate,
            result.DuplicateWarning);
    }
}
