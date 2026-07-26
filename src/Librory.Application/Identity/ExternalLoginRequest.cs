using Librory.Domain.Models;

namespace Librory.Application.Identity;

public sealed record ExternalLoginRequest(
    ExternalIdentityProvider Provider,
    string ProviderSubject,
    string? Email,
    string? DisplayName,
    string SuggestedFamilyName,
    string SuggestedMemberDisplayName,
    PreferredLanguage PreferredLanguage);
