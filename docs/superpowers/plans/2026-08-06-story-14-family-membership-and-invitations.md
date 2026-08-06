# Story 14 Family Membership and Invitations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Support personal and shared families, multi-family accounts, placeholder members, and secure Koviva-aligned email invitation onboarding.

**Architecture:** Keep login identity at account scope and keep display name, role, language, profile ownership, and library membership at family-member scope. Persist the selected family/member in the authenticated session so existing `/api/family/current/...` endpoints continue to operate against one verified active membership. Implement invitations as a separate hashed-token workflow that either creates a new membership or binds a placeholder membership after the authenticated email matches.

**Tech Stack:** ASP.NET Core minimal APIs, .NET 10, EF Core with PostgreSQL, cookie authentication, xUnit API/Application/Domain tests, generated EF Core migrations.

## Global Constraints

- Every first registration creates one personal family and one admin membership.
- Every first registration creates a personal family; accepting an invitation adds a second family membership when applicable.
- One login account may have memberships in multiple families; family data remains isolated by membership.
- Invitation tokens are hashed at rest, single-use, and expire after seven days.
- Invitation status values are `Pending`, `Accepted`, `Expired`, `Revoked`, and `Superseded`.
- Only administrators can mutate family membership or invitations; new invitations always create `Member` role memberships.
- Deactivation preserves historical data and prevents new family targeting.
- Keep the Koviva-compatible frontend token URL and API route shape, but redact tokens from logs and require restrictive referrer policy and HTTPS outside local development.
- Do not add an email provider implementation in this story; use an application notification seam and make invitation creation testable without external delivery.

---

### Task 1: Add account, membership, and invitation domain state

**Files:**
- Create: `src/Librory.Domain/Models/UserAccount.cs`
- Create: `src/Librory.Domain/Models/FamilyInvitation.cs`
- Create: `src/Librory.Domain/Models/FamilyInvitationStatus.cs`
- Modify: `src/Librory.Domain/Models/Member.cs`
- Modify: `src/Librory.Domain/Models/Family.cs`
- Modify: `src/Librory.Domain/Models/ExternalIdentity.cs`
- Create: `tests/Librory.Domain.Tests/FamilyInvitationTests.cs`
- Modify: `tests/Librory.Domain.Tests/FamilyMemberPersistenceTests.cs`
- Modify: `tests/Librory.Domain.Tests/ExternalIdentityTests.cs` (create if the existing identity tests are not in this file)

**Interfaces:**
- `UserAccount` owns the stable account id, normalized email, and external identity collection.
- `Member` owns family-scoped display name, role, preferred UI language, active state, and nullable `UserAccountId`.
- `FamilyInvitation` owns family id, optional target member id, normalized email, token hash, status, expiry, and audit ids/timestamps.
- Domain methods must expose `Deactivate`, `Reactivate`, `LinkAccount`, and invitation state transitions with validation.

- [ ] **Step 1: Write failing domain tests** for linking an account to one or more family members, rejecting a second account link to the same member, deactivating/reactivating a member, invitation expiry, single-use acceptance, revocation, and supersession.
- [ ] **Step 2: Run focused domain tests**

Run: `dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj --no-restore --filter "FullyQualifiedName~FamilyInvitationTests|FullyQualifiedName~FamilyMemberPersistenceTests" -v minimal`

Expected: FAIL because the new account, invitation, and lifecycle APIs do not exist.

- [ ] **Step 3: Implement the domain types and invariants** without persistence concerns. Normalize emails in the application layer, but make domain transitions reject invalid ownership, already-final invitation states, and inactive target members.
- [ ] **Step 4: Run the focused tests again** and confirm all new and existing tests pass.
- [ ] **Step 5: Commit**

```bash
git add src/Librory.Domain tests/Librory.Domain.Tests
git commit -m "feat: add family membership and invitation domain state"
```

### Task 2: Persist accounts, memberships, and invitations

**Files:**
- Modify: `src/Librory.Infrastructure/Persistence/LibroryDbContext.cs`
- Create: `src/Librory.Infrastructure/Persistence/Configurations/UserAccountConfiguration.cs`
- Modify: `src/Librory.Infrastructure/Persistence/Configurations/MemberConfiguration.cs`
- Create: `src/Librory.Infrastructure/Persistence/Configurations/FamilyInvitationConfiguration.cs`
- Modify: `src/Librory.Infrastructure/Persistence/Configurations/FamilyConfiguration.cs`
- Create via EF tooling: `src/Librory.Infrastructure/Persistence/Migrations/*_AddFamilyMembershipsAndInvitations.cs`
- Modify through EF tooling: `src/Librory.Infrastructure/Persistence/Migrations/LibroryDbContextModelSnapshot.cs`
- Modify: `tests/Librory.Api.Tests/LibroryDbContextModelTests.cs`

**Interfaces:**
- Add `DbSet<UserAccount> UserAccounts` and `DbSet<FamilyInvitation> FamilyInvitations`.
- Enforce unique normalized account email when present, unique external provider/subject, one active account link per membership, and indexes for invitation token hash, family/email/status, expiry, and target member.
- Preserve cascade behavior for family-owned records while preventing accidental cascade deletion of a user account when a membership is removed.

- [ ] **Step 1: Add EF model tests** for required fields, unique keys, invitation indexes, and family/member relationships.
- [ ] **Step 2: Run model tests to capture the failing schema assertions**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --no-restore --filter FullyQualifiedName~LibroryDbContextModelTests -v minimal`

Expected: FAIL until the new entities and configurations are registered.

- [ ] **Step 3: Implement configurations and DbContext registration** following the existing explicit configuration style.
- [ ] **Step 4: Generate the migration with EF tooling**

Run: `dotnet ef migrations add AddFamilyMembershipsAndInvitations --project src/Librory.Infrastructure/Librory.Infrastructure.csproj --startup-project src/Librory.Api/Librory.Api.csproj --output-dir Persistence/Migrations`

Expected: a generated migration and updated snapshot with no hand-authored migration history.

- [ ] **Step 5: Run the model and PostgreSQL schema tests**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~LibroryDbContextModelTests|FullyQualifiedName~ApiIntegrationTests" -v minimal`

- [ ] **Step 6: Commit**

```bash
git add src/Librory.Infrastructure tests/Librory.Api.Tests
git commit -m "feat: persist family memberships and invitations"
```

### Task 3: Make login and active family context account-aware

**Files:**
- Modify: `src/Librory.Application/Identity/ExternalLoginRequest.cs`
- Modify: `src/Librory.Application/Identity/ExternalLoginResult.cs`
- Modify: `src/Librory.Application/Identity/IExternalLoginService.cs`
- Modify: `src/Librory.Infrastructure/Identity/ExternalLoginService.cs`
- Modify: `src/Librory.Application/Families/FirstLoginFamilyBootstrapper.cs`
- Modify: `src/Librory.Application/Families/CurrentFamilyContext.cs`
- Modify: `src/Librory.Application/Families/CurrentFamilyContextClaimTypes.cs`
- Modify: `src/Librory.Application/Families/CurrentFamilyContextResolver.cs`
- Modify: `src/Librory.Api/Authentication/AuthenticationSessionFactory.cs`
- Modify: `src/Librory.Api/Endpoints/AuthEndpoints.cs`
- Modify: `src/Librory.Api/Endpoints/DevAuthEndpoints.cs`
- Modify: `tests/Librory.Application.Tests/ExternalIdentityResolverTests.cs`
- Modify: `tests/Librory.Api.Tests/ExternalLoginServiceTests.cs`
- Modify: `tests/Librory.Api.Tests/AuthEndpointsTests.cs`

**Interfaces:**
- External login resolves one account by provider/subject, then chooses one active membership as the initial active family.
- Login result includes account id, active family id, active member id, and available family summaries.
- Authentication claims include account id plus active family/member claims; the server revalidates the active membership before family-scoped operations.
- Existing direct-login behavior remains compatible for a single-family account.

- [ ] **Step 1: Add failing tests** for first login creating account plus personal family, repeat login reusing the same account, and one account resolving to multiple family memberships.
- [ ] **Step 2: Implement account-aware external login and first-login bootstrap**. Existing external identity records must resolve to the account before selecting a membership.
- [ ] **Step 3: Add active-family claim/session creation and revalidation** without changing existing `/api/family/current` consumers.
- [ ] **Step 4: Update development auth** so tests can create deterministic accounts and memberships without real OAuth.
- [ ] **Step 5: Run authentication tests**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~AuthEndpointsTests|FullyQualifiedName~ExternalLoginServiceTests" -v minimal`

- [ ] **Step 6: Commit**

```bash
git add src/Librory.Application src/Librory.Infrastructure/Identity src/Librory.Api/Authentication src/Librory.Api/Endpoints tests
git commit -m "feat: make authentication account and membership aware"
```

### Task 4: Add family listing, switching, and member management APIs

**Files:**
- Create: `src/Librory.Api/Contracts/FamilyListResponse.cs`
- Create: `src/Librory.Api/Contracts/FamilyMemberResponse.cs`
- Create: `src/Librory.Api/Contracts/CreateFamilyMemberRequest.cs`
- Create: `src/Librory.Api/Contracts/UpdateFamilyMemberRequest.cs`
- Create: `src/Librory.Application/Families/FamilyMembershipService.cs`
- Modify: `src/Librory.Api/Endpoints/FamilyEndpoints.cs`
- Modify: `src/Librory.Api/Contracts/CurrentFamilyResponse.cs`
- Create: `tests/Librory.Api.Tests/FamilyMembershipEndpointsTests.cs`

**Interfaces:**
- `GET /api/families` returns only families with active memberships for the authenticated account.
- `POST /api/families/{familyId}/select` verifies membership, updates the auth session, and returns the selected current-family response.
- `GET /api/family/current/members` returns active and optionally deactivated members without private profile data.
- `POST /api/family/current/members` creates an admin-managed placeholder member with default `Member` role.
- `PATCH /api/family/current/members/{memberId}` updates display name and UI language.
- `POST .../{memberId}/deactivate` and `POST .../{memberId}/reactivate` preserve history and require admin role.

- [ ] **Step 1: Write API tests** for single-family listing, multi-family filtering, unauthorized family selection, placeholder creation, duplicate names, and deactivation behavior.
- [ ] **Step 2: Implement membership service and response contracts** with family-boundary checks in one place.
- [ ] **Step 3: Implement endpoints and session refresh** for family selection.
- [ ] **Step 4: Run the focused API tests**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --no-restore --filter FullyQualifiedName~FamilyMembershipEndpointsTests -v minimal`

- [ ] **Step 5: Commit**

```bash
git add src/Librory.Api src/Librory.Application/Families tests/Librory.Api.Tests
git commit -m "feat: add family and member management APIs"
```

### Task 5: Implement Koviva-aligned invitation lifecycle

**Files:**
- Create: `src/Librory.Application/Invitations/FamilyInvitationService.cs`
- Create: `src/Librory.Application/Invitations/IFamilyInvitationService.cs`
- Create: `src/Librory.Application/Invitations/FamilyInvitationDto.cs`
- Create: `src/Librory.Application/Invitations/IInvitationNotificationService.cs`
- Create: `src/Librory.Infrastructure/Invitations/FamilyInvitationNotificationService.cs`
- Create: `src/Librory.Api/Contracts/FamilyInvitationRequest.cs`
- Create: `src/Librory.Api/Contracts/FamilyInvitationResponse.cs`
- Create: `src/Librory.Api/Endpoints/FamilyInvitationEndpoints.cs`
- Modify: `src/Librory.Infrastructure/DependencyInjection.cs`
- Modify: `src/Librory.Api/Program.cs`
- Create: `tests/Librory.Application.Tests/FamilyInvitationServiceTests.cs`
- Create: `tests/Librory.Api.Tests/FamilyInvitationEndpointsTests.cs`

**Interfaces:**
- Admin routes: list, create new-member invitation, invite existing placeholder, resend, revoke.
- Public route shape matching Koviva: `GET /api/family-invitations/{token}` and authenticated `POST /api/family-invitations/{token}/accept`.
- Tokens are 32 random bytes encoded URL-safe, hashed with SHA-256 before persistence, and never returned after creation except inside the frontend accept URL.
- Accept compares normalized invitation email with the authenticated account email, rejects expired/revoked/superseded/accepted tokens, and runs in a transaction.
- Accept creates a new `Member` when no target member exists, or links the existing placeholder member when target member exists.
- A notification seam records/queues the generated frontend URL; no real email transport is introduced.

- [ ] **Step 1: Write service tests** for create, duplicate pending supersession, resend, revoke, lookup, email mismatch, expiry, single-use acceptance, new-member acceptance, and placeholder binding.
- [ ] **Step 2: Implement normalization, token hashing, status transitions, and notification seam**.
- [ ] **Step 3: Write endpoint tests** for admin authorization, family scoping, public lookup, authenticated acceptance, and token non-disclosure.
- [ ] **Step 4: Implement admin and public minimal API endpoints** with token redaction in request logging and response payloads that never expose token hashes.
- [ ] **Step 5: Run invitation tests**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --no-restore --filter FullyQualifiedName~FamilyInvitationEndpointsTests -v minimal`

- [ ] **Step 6: Commit**

```bash
git add src/Librory.Application/Invitations src/Librory.Infrastructure/Invitations src/Librory.Api tests
git commit -m "feat: add family invitation onboarding"
```

### Task 6: Update API documentation and run the full verification suite

**Files:**
- Modify: `docs/api-reference.md`
- Modify: `docs/frontend-integration-guide.md`
- Create: `docs/devlog/2026-08-06-story-14-family-membership-and-invitations.md`
- Modify: `docs/backend-story-map.md` to mark Story 14 delivered only after all implementation tasks pass.

- [ ] **Step 1: Document family list/switch, member management, and invitation flows** using the actual route and response shapes.
- [ ] **Step 2: Run the complete backend suite**

Run: `dotnet test Librory.sln --no-restore -v minimal`

Expected: all Domain, Application, and API tests pass with zero failures.

- [ ] **Step 3: Run `git diff --check` and inspect the final status**.
- [ ] **Step 4: Commit documentation and verification updates**

```bash
git add docs
git commit -m "docs: record story 14 family membership delivery"
```
