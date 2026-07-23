namespace Librory.Domain.Models;

public sealed record ExternalIdentity(
    ExternalIdentityProvider Provider,
    string ProviderSubject,
    string? Email = null,
    string? DisplayName = null,
    DateTimeOffset? LinkedAt = null);
