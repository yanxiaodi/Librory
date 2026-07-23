using Librory.Domain.Models;

namespace Librory.Application.Families;

public sealed record FamilyBootstrapResult(
    Family Family,
    Member InitialMember);
