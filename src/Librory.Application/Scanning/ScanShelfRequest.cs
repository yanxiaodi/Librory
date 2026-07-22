namespace Librory.Application.Scanning;

public sealed record ScanShelfRequest(
    Guid FamilyId,
    string? PreferredLanguage,
    string ShelfPhotoPath);
