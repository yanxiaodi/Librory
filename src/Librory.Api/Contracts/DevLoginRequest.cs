using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record DevLoginRequest(
    string FamilyName,
    string MemberDisplayName,
    PreferredLanguage PreferredLanguage = PreferredLanguage.English);
