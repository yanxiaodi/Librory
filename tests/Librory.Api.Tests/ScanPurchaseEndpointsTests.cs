using System.Net;
using System.Net.Http.Json;
using Librory.Api.Contracts;
using Librory.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;
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
}
