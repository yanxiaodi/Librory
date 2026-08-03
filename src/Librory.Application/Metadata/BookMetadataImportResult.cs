using Librory.Domain.Models;

namespace Librory.Application.Metadata;

public sealed record BookMetadataImportResult(
    BookWork Work,
    bool CreatedNew);
