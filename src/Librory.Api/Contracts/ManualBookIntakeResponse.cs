namespace Librory.Api.Contracts;

public sealed record ManualBookIntakeResponse(
    BookCopyResponse Copy,
    bool HasPotentialDuplicate,
    string? DuplicateWarning);
