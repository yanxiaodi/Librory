using System.Net;
using System.Data.Common;
using System.Net.Http.Json;
using Librory.Api.Contracts;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Librory.Api.Tests;

public sealed class ScanPurchaseEndpointsTests
{
    [Fact]
    public async Task Purchase_creates_one_copy_for_another_family_member_and_replay_is_idempotent()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var login = await LoginAsync(client, "Purchase Family", "Purchaser");
        var owner = await LoginAsync(client, "Purchase Family", "Owner");
        await LoginAsync(client, "Purchase Family", "Purchaser");

        var session = await CreateSessionAsync(client, "Matilda");
        var candidate = Assert.Single(session.Candidates);
        var purchaseRequestId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                purchaseRequestId,
                owner.MemberId,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Matilda",
                ManualAuthor: "Roald Dahl"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.NotNull(created);
        Assert.Equal(owner.MemberId, created!.Copy.MemberId);
        Assert.Equal(login.MemberId, created.Copy.PurchasedByMemberId);
        Assert.True(created.IsProvisional);
        Assert.Equal(BookCopyDuplicateStatus.ConfirmedUnique, created.DuplicateStatus);

        var replayResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                purchaseRequestId,
                owner.MemberId,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Matilda",
                ManualAuthor: "Roald Dahl"));

        Assert.Equal(HttpStatusCode.Created, replayResponse.StatusCode);
        var replay = await replayResponse.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.NotNull(replay);
        Assert.True(replay!.IsReplay);
        Assert.Equal(created.Copy.BookCopyId, replay.Copy.BookCopyId);

        var sessionResponse = await client.GetAsync($"/api/family/current/scan-sessions/{session.ScanSessionId}");
        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        var reloaded = await sessionResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(reloaded);
        Assert.Equal(PurchaseStatus.Purchased, reloaded!.Candidates.Single().PurchaseStatus);

        var secondRequest = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                owner.MemberId,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Matilda"));

        Assert.Equal(HttpStatusCode.BadRequest, secondRequest.StatusCode);
    }

    [Fact]
    public async Task Purchase_rejects_reusing_a_request_id_for_another_candidate()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Request Identity Family", "Purchaser");

        var firstSession = await CreateSessionAsync(client, "First book");
        var firstCandidate = Assert.Single(firstSession.Candidates);
        var purchaseRequestId = Guid.NewGuid();
        var firstPurchase = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{firstSession.ScanSessionId}/candidates/{firstCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                purchaseRequestId,
                firstSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "First book"));
        Assert.Equal(HttpStatusCode.Created, firstPurchase.StatusCode);

        var secondSession = await CreateSessionAsync(client, "Second book");
        var secondCandidate = Assert.Single(secondSession.Candidates);
        var secondPurchase = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{secondSession.ScanSessionId}/candidates/{secondCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                purchaseRequestId,
                secondSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Second book"));

        Assert.Equal(HttpStatusCode.BadRequest, secondPurchase.StatusCode);
    }

    [Fact]
    public async Task Purchase_rejects_invalid_selected_metadata_at_the_api_boundary()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Metadata Validation Family", "Purchaser");

        var session = await CreateSessionAsync(client, "Dune");
        var candidate = Assert.Single(session.Candidates);
        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                new BookMetadataImportCandidateRequest(
                    null!,
                    "source-1",
                    "Dune",
                    null,
                    ["Frank Herbert"],
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_records_manual_publication_year_provenance_when_it_overrides_provider_metadata()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Provenance Family", "Purchaser");

        var session = await CreateSessionAsync(client, "Dune");
        var candidate = Assert.Single(session.Candidates);
        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                new BookMetadataImportCandidateRequest(
                    "GoogleBooks",
                    "volume-1",
                    "Dune",
                    null,
                    ["Frank Herbert"],
                    "Ace",
                    "1965",
                    "en",
                    null,
                    null,
                    "9780441013593",
                    null,
                    null),
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                PublicationYear: 2024));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var purchase = await response.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.NotNull(purchase);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
        var edition = await db.BookEditions.FindAsync(purchase!.BookEditionId);
        Assert.NotNull(edition);
        Assert.Equal(2024, edition!.PublicationYear);
        Assert.Equal("Manual", edition.PublicationYearProvenance?.Source);
    }

    [Fact]
    public async Task Purchased_provisional_edition_can_be_confirmed_through_the_api()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Confirm Edition Family", "Purchaser");

        var session = await CreateSessionAsync(client, "Dune");
        var candidate = Assert.Single(session.Candidates);
        var purchaseResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune"));
        var purchase = await purchaseResponse.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.NotNull(purchase);
        Assert.True(purchase!.IsProvisional);

        var response = await client.PutAsJsonAsync(
            $"/api/family/current/book-editions/{purchase.BookEditionId}/version",
            new UpdateBookEditionVersionRequest("9780441013593", "Paperback", 1965));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var edition = await response.Content.ReadFromJsonAsync<BookEditionResponse>();
        Assert.NotNull(edition);
        Assert.False(edition!.IsProvisional);
    }

    [Fact]
    public async Task Purchase_retries_after_a_serializable_postgres_failure()
    {
        var interceptor = new OneShotSerializationFailureInterceptor();
        await using var factory = await ApiFactory.CreateAsync(interceptor);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Retry Family", "Purchaser");

        var session = await CreateSessionAsync(client, "Dune");
        var candidate = Assert.Single(session.Candidates);
        interceptor.Arm();

        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune"));

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal(1, interceptor.InjectedFailures);
    }

    [Fact]
    public async Task Purchase_requires_duplicate_confirmation_then_allows_confirmed_duplicate()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Duplicate Purchase Family", "Purchaser");

        var firstSession = await CreateSessionAsync(client, "Charlotte's Web");
        var firstCandidate = Assert.Single(firstSession.Candidates);
        var firstPurchase = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{firstSession.ScanSessionId}/candidates/{firstCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                firstSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Charlotte's Web",
                ManualAuthor: "E. B. White"));
        Assert.Equal(HttpStatusCode.Created, firstPurchase.StatusCode);

        var secondSession = await CreateSessionAsync(client, "Charlotte's Web");
        var secondCandidate = Assert.Single(secondSession.Candidates);
        var purchaseRequestId = Guid.NewGuid();
        var warning = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{secondSession.ScanSessionId}/candidates/{secondCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                purchaseRequestId,
                secondSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Charlotte's Web",
                ManualAuthor: "E. B. White"));

        Assert.Equal(HttpStatusCode.Conflict, warning.StatusCode);
        var warningPayload = await warning.Content.ReadFromJsonAsync<DuplicateConfirmationResponse>();
        Assert.NotNull(warningPayload);
        Assert.NotEmpty(warningPayload!.Matches);

        var confirmed = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{secondSession.ScanSessionId}/candidates/{secondCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                purchaseRequestId,
                secondSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                DuplicateStatus: BookCopyDuplicateStatus.ConfirmedDuplicate,
                ManualTitle: "Charlotte's Web",
                ManualAuthor: "E. B. White"));

        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
        var confirmedPayload = await confirmed.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.NotNull(confirmedPayload);
        Assert.Equal(BookCopyDuplicateStatus.ConfirmedDuplicate, confirmedPayload!.DuplicateStatus);
    }

    [Fact]
    public async Task New_work_resolution_does_not_reuse_an_existing_isbn()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "New Work ISBN Family", "Purchaser");

        var firstSession = await CreateSessionAsync(client, "First title");
        var firstCandidate = Assert.Single(firstSession.Candidates);
        var firstResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{firstSession.ScanSessionId}/candidates/{firstCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                firstSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "First title",
                Isbn: "9780000000001"));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<ScanPurchaseResponse>();

        var secondSession = await CreateSessionAsync(client, "Second title");
        var secondCandidate = Assert.Single(secondSession.Candidates);
        var secondResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{secondSession.ScanSessionId}/candidates/{secondCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                secondSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                DuplicateStatus: BookCopyDuplicateStatus.ConfirmedDuplicate,
                ManualTitle: "Second title",
                Isbn: "9780000000001"));

        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var second = await secondResponse.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.Work.BookWorkId, second!.Work.BookWorkId);
    }

    [Fact]
    public async Task Duplicate_resolution_can_reuse_the_selected_edition_or_add_an_edition_to_the_selected_work()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Resolution Branch Family", "Purchaser");

        var firstSession = await CreateSessionAsync(client, "Dune");
        var firstCandidate = Assert.Single(firstSession.Candidates);
        var firstResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{firstSession.ScanSessionId}/candidates/{firstCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                firstSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune",
                ManualAuthor: "Frank Herbert",
                Isbn: "9780441013593"));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var existingEditionSession = await CreateSessionAsync(client, "Dune");
        var existingEditionCandidate = Assert.Single(existingEditionSession.Candidates);
        var existingEditionWarning = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{existingEditionSession.ScanSessionId}/candidates/{existingEditionCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                existingEditionSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune",
                Isbn: "9780441013593"));
        var existingEditionMatches = await existingEditionWarning.Content.ReadFromJsonAsync<DuplicateConfirmationResponse>();
        Assert.Equal(HttpStatusCode.Conflict, existingEditionWarning.StatusCode);
        Assert.NotEmpty(existingEditionMatches!.Matches);
        var existingEditionMatch = existingEditionMatches.Matches[0];

        var existingEditionPurchase = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{existingEditionSession.ScanSessionId}/candidates/{existingEditionCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                existingEditionSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.ExistingEdition,
                DuplicateStatus: BookCopyDuplicateStatus.ConfirmedDuplicate,
                ExistingBookEditionId: existingEditionMatch.BookEditionId,
                ManualTitle: "Dune"));
        var existingEditionResult = await existingEditionPurchase.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.Equal(HttpStatusCode.Created, existingEditionPurchase.StatusCode);
        Assert.Equal(existingEditionMatch.BookEditionId, existingEditionResult!.BookEditionId);

        var sameWorkSession = await CreateSessionAsync(client, "Dune");
        var sameWorkCandidate = Assert.Single(sameWorkSession.Candidates);
        var sameWorkWarning = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{sameWorkSession.ScanSessionId}/candidates/{sameWorkCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                sameWorkSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune",
                Isbn: "9780441013593"));
        var sameWorkMatches = await sameWorkWarning.Content.ReadFromJsonAsync<DuplicateConfirmationResponse>();
        Assert.Equal(HttpStatusCode.Conflict, sameWorkWarning.StatusCode);
        Assert.NotEmpty(sameWorkMatches!.Matches);
        var sameWorkMatch = sameWorkMatches.Matches[0];

        var sameWorkPurchase = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{sameWorkSession.ScanSessionId}/candidates/{sameWorkCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                sameWorkSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.SameWorkNewEdition,
                DuplicateStatus: BookCopyDuplicateStatus.ConfirmedDuplicate,
                ExistingBookWorkId: sameWorkMatch.BookWorkId,
                ManualTitle: "Dune",
                Isbn: "9780441013593"));
        var sameWorkResult = await sameWorkPurchase.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.Equal(HttpStatusCode.Created, sameWorkPurchase.StatusCode);
        Assert.Equal(sameWorkMatch.BookWorkId, sameWorkResult!.Work.BookWorkId);
        Assert.NotEqual(sameWorkMatch.BookEditionId, sameWorkResult.BookEditionId);
    }

    [Fact]
    public async Task Purchase_rejects_missing_or_unknown_duplicate_resolution_ids()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Resolution Validation Family", "Purchaser");
        var session = await CreateSessionAsync(client, "Matilda");
        var candidate = Assert.Single(session.Candidates);
        var url = $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase";

        var missingId = await client.PostAsJsonAsync(url, new ConfirmScanPurchaseRequest(
            Guid.NewGuid(),
            session.TargetMemberId!.Value,
            DuplicateResolution: Librory.Application.Intake.DuplicateResolution.ExistingEdition,
            ManualTitle: "Matilda"));
        Assert.Equal(HttpStatusCode.BadRequest, missingId.StatusCode);

        var unknownValue = await client.PostAsJsonAsync(url, new
        {
            purchaseRequestId = Guid.NewGuid(),
            ownerMemberId = session.TargetMemberId!.Value,
            duplicateResolution = 99,
            manualTitle = "Matilda",
        });
        Assert.Equal(HttpStatusCode.BadRequest, unknownValue.StatusCode);
    }

    [Fact]
    public async Task Purchased_candidates_cannot_be_resolved_or_discarded_again()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchased Candidate Mutation Family", "Purchaser");
        var session = await CreateSessionAsync(client, "Matilda");
        var candidate = Assert.Single(session.Candidates);
        var url = $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}";

        var purchase = await client.PostAsJsonAsync($"{url}/purchase", new ConfirmScanPurchaseRequest(
            Guid.NewGuid(),
            session.TargetMemberId!.Value,
            DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
            ManualTitle: "Matilda"));
        Assert.Equal(HttpStatusCode.Created, purchase.StatusCode);

        var resolve = await client.PostAsJsonAsync($"{url}/resolve", new ResolveScanCandidateRequest("Different title"));
        Assert.Equal(HttpStatusCode.BadRequest, resolve.StatusCode);

        var discard = await client.DeleteAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, discard.StatusCode);
    }

    [Fact]
    public async Task Scan_metadata_matches_survive_session_reload()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Snapshot Family", "Owner");

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf.jpg",
                Candidates:
                [
                    new CreateScanCandidateRequest(
                        "Charlotte's Web",
                        "High",
                        RecognitionEvidence: "Title and author matched",
                        RecognitionRank: 1,
                        MetadataMatches:
                        [
                            new BookMetadataImportCandidateRequest(
                                "GoogleBooks",
                                "volume-1",
                                "Charlotte's Web",
                                null,
                                ["E. B. White"],
                                null,
                                "1952",
                                "en",
                                null,
                                null,
                                "9780061124952",
                                null,
                                null),
                        ])]));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);
        Assert.Equal("Title and author matched", created!.Candidates.Single().MetadataSnapshot!.EvidenceText);

        var reload = await client.GetAsync($"/api/family/current/scan-sessions/{created.ScanSessionId}");
        Assert.Equal(HttpStatusCode.OK, reload.StatusCode);
        var fetched = await reload.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(fetched);
        Assert.Equal("volume-1", fetched!.Candidates.Single().MetadataSnapshot!.Matches.Single().SourceId);
    }

    [Fact]
    public async Task Purchase_allows_assigning_a_copy_to_a_deactivated_family_member()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        await LoginAsync(client, "Inactive Owner Family", "Purchaser");
        var owner = await LoginAsync(client, "Inactive Owner Family", "Former Reader");
        await LoginAsync(client, "Inactive Owner Family", "Purchaser");

        var deactivate = await client.PostAsync($"/api/family/current/members/{owner.MemberId}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var session = await CreateSessionAsync(client, "The Hobbit");
        var candidate = Assert.Single(session.Candidates);
        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                owner.MemberId,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "The Hobbit"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var purchase = await response.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.NotNull(purchase);
        Assert.Equal(owner.MemberId, purchase!.Copy.MemberId);
    }

    [Fact]
    public async Task Private_notes_and_consent_are_only_visible_and_editable_by_the_profile_owner()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var owner = await LoginAsync(client, "Private Profile Family", "Profile Owner");
        var ownUpdate = await client.PutAsJsonAsync(
            "/api/family/current/recommendation-profile",
            new UpsertRecommendationProfileRequest(
                preferenceNotes: "Private reading note",
                usePrivateNotesInFamilyRecommendations: true));
        Assert.Equal(HttpStatusCode.OK, ownUpdate.StatusCode);
        var ownPayload = await ownUpdate.Content.ReadFromJsonAsync<RecommendationProfileResponse>();
        Assert.Equal("Private reading note", ownPayload!.PreferenceNotes);
        Assert.True(ownPayload.UsePrivateNotesInFamilyRecommendations);

        var other = await LoginAsync(client, "Private Profile Family", "Another Member");
        var read = await client.GetAsync($"/api/family/current/members/{owner.MemberId}/recommendation-profile");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var hidden = await read.Content.ReadFromJsonAsync<RecommendationProfileResponse>();
        Assert.NotNull(hidden);
        Assert.Null(hidden!.PreferenceNotes);
        Assert.Null(hidden.UsePrivateNotesInFamilyRecommendations);

        var forbidden = await client.PutAsJsonAsync(
            $"/api/family/current/members/{owner.MemberId}/recommendation-profile",
            new UpsertRecommendationProfileRequest(preferenceNotes: "Attempted overwrite"));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.NotEqual(owner.MemberId, other.MemberId);
    }

    private static async Task<DevLoginResponse> LoginAsync(HttpClient client, string familyName, string memberName)
    {
        var response = await client.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest(familyName, memberName, PreferredLanguage.English));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DevLoginResponse>())!;
    }

    private static async Task<ScanSessionResponse> CreateSessionAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf.jpg",
                Candidates: [new CreateScanCandidateRequest(title, "High")]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ScanSessionResponse>())!;
    }

    private sealed class OneShotSerializationFailureInterceptor : DbCommandInterceptor
    {
        private int _armed;
        private int _remainingFailures;

        public int InjectedFailures { get; private set; }

        public void Arm()
        {
            _remainingFailures = 1;
            _armed = 1;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            ThrowIfArmed(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ThrowIfArmed(command);
            return ValueTask.FromResult(result);
        }

        private void ThrowIfArmed(DbCommand command)
        {
            if (Volatile.Read(ref _armed) == 1
                && command.CommandText.Contains("scan_candidates", StringComparison.OrdinalIgnoreCase)
                && Interlocked.Decrement(ref _remainingFailures) >= 0)
            {
                InjectedFailures++;
                Volatile.Write(ref _armed, 0);
                throw new PostgresException("Serialization failure", "ERROR", "ERROR", "40001");
            }
        }
    }
}
