namespace Librory.Application.Metadata;

public sealed record BookMetadataImportOptions(
    bool AllowProvisionalEdition = false,
    string? Isbn = null,
    string? Format = null,
    int? PublicationYear = null);
