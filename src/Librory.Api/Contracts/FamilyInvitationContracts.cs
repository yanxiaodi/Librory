using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record CreateFamilyInvitationRequest(string Email, Guid? TargetMemberId = null);

public sealed record FamilyInvitationResponse(Guid InvitationId, Guid FamilyId, Guid? TargetMemberId, string Email, FamilyInvitationStatus Status, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
