using Librory.Application.Recognition;

namespace Librory.Infrastructure.Recognition;

public sealed class BookTitleCandidateRanker
{
    private static readonly string[] NoiseWords =
    [
        "author",
        "publisher",
        "copyright",
        "isbn",
        "edition",
        "series",
        "masterpiece",
        "inspired by",
        "bestseller",
        "available now",
    ];

    public IReadOnlyList<BookRecognitionCandidateDto> Rank(IEnumerable<RecognizedTextBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        return blocks
            .Select(block => CreateCandidate(block))
            .Where(candidate => candidate is not null)
            .OrderByDescending(candidate => candidate!.Rank)
            .ThenBy(candidate => candidate!.DisplayTitle, StringComparer.OrdinalIgnoreCase)
            .Select(candidate => candidate!)
            .ToList();
    }

    private static BookRecognitionCandidateDto? CreateCandidate(RecognizedTextBlock block)
    {
        var displayTitle = Normalize(block.Text);
        if (string.IsNullOrWhiteSpace(displayTitle))
        {
            return null;
        }

        var score = block.Confidence;
        var wordCount = displayTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        var length = displayTitle.Length;

        if (length is >= 3 and <= 80)
        {
            score += 0.10m;
        }

        if (wordCount is >= 1 and <= 8)
        {
            score += 0.10m;
        }

        if (block.IsVertical)
        {
            score += 0.12m;
        }

        if (displayTitle.Any(char.IsDigit))
        {
            score -= 0.15m;
        }

        if (displayTitle.Count(char.IsPunctuation) > 2)
        {
            score -= 0.15m;
        }

        if (NoiseWords.Any(noiseWord => displayTitle.Contains(noiseWord, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        if (LooksLikeAuthorLine(displayTitle))
        {
            score -= 0.35m;
        }

        if (score < 0.35m)
        {
            return null;
        }

        var rank = (int)Math.Round(score * 1000m, MidpointRounding.AwayFromZero);
        return new BookRecognitionCandidateDto(
            Guid.NewGuid(),
            displayTitle,
            displayTitle,
            rank,
            []);
    }

    private static string Normalize(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : string.Join(' ', text.Split((string[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static bool LooksLikeAuthorLine(string displayTitle)
    {
        var words = displayTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length is < 2 or > 4)
        {
            return false;
        }

        if (words.Any(word =>
                word.Equals("the", StringComparison.OrdinalIgnoreCase) ||
                word.Equals("of", StringComparison.OrdinalIgnoreCase) ||
                word.Equals("and", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return words.All(word => char.IsUpper(word[0]));
    }
}
