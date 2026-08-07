namespace Librory.Domain.Models;

public sealed record ScanLanguageContext(
    PreferredLanguage? DominantLanguage,
    bool IsMixed);
