namespace Librory.Domain.Models;

public sealed record LocalizedText(
    string English,
    string? Chinese = null);
