namespace Librory.Domain.Models;

public static class RecommendationCategoryCatalog
{
    public static readonly IReadOnlyList<string> DefaultGenres =
    [
        "Adventure",
        "Animals",
        "Biography",
        "Coming of Age",
        "Family",
        "Fantasy",
        "Friendship",
        "Graphic Novel",
        "Historical Fiction",
        "Humor",
        "Mystery",
        "Nonfiction",
        "Poetry",
        "Picture Book",
        "Realistic Fiction",
        "Science Fiction",
    ];

    public static readonly IReadOnlyList<string> DefaultStyles =
    [
        "Character-driven",
        "Fast-paced",
        "Illustrated",
        "Lyric",
        "Quiet",
        "Reflective",
        "Series",
        "Standalone",
    ];
}
