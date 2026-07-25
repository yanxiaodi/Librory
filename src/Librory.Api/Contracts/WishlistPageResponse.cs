using Librory.Application.Wishlist;

namespace Librory.Api.Contracts;

public sealed record WishlistPageResponse(
    IReadOnlyList<WishlistItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
