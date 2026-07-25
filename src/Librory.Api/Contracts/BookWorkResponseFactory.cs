using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public static class BookWorkResponseFactory
{
    public static BookWorkResponse Create(BookWork work)
    {
        ArgumentNullException.ThrowIfNull(work);

        var editions = work.Editions
            .Select(edition => new BookEditionResponse(
                edition.Id,
                edition.Isbn,
                edition.Format,
                edition.PublicationYear))
            .ToList();

        return new BookWorkResponse(
            work.Id,
            work.CanonicalTitle,
            work.CanonicalAuthor,
            editions);
    }
}
