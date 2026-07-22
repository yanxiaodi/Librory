namespace Librory.Application.Families;

public sealed record FamilySummaryDto(
    Guid FamilyId,
    string FamilyName,
    IReadOnlyList<string> MemberNames,
    int BookCount);
