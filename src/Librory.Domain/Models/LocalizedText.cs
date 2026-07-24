namespace Librory.Domain.Models;

public sealed record LocalizedText(
    string English,
    string? Chinese = null)
{
    public string GetValue(PreferredLanguage preferredLanguage)
    {
        return preferredLanguage switch
        {
            PreferredLanguage.Chinese when !string.IsNullOrWhiteSpace(Chinese) => Chinese!,
            _ => English,
        };
    }
}
