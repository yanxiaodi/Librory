using System.Data;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Librory.Api.Contracts;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
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
        var reloadedCandidate = Assert.Single(reloaded!.Candidates);
        Assert.Equal(PurchaseStatus.Purchased, reloadedCandidate.PurchaseStatus);
        Assert.NotNull(reloadedCandidate.Purchase);
        var replayPurchase = reloadedCandidate.Purchase!;
        Assert.Equal(created.BookEditionId, replayPurchase.BookEditionId);
        Assert.Equal(created.IsProvisional, replayPurchase.IsProvisional);
        Assert.Contains(
            replayPurchase.Work.Editions,
            edition => edition.BookEditionId == created.BookEditionId);

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
    public async Task Purchase_persists_the_selected_metadata_for_session_reload()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Selected Metadata Snapshot Family", "Purchaser");

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf.jpg",
                Candidates:
                [
                    new CreateScanCandidateRequest(
                        "Dune",
                        "High",
                        MetadataMatches:
                        [
                            new BookMetadataImportCandidateRequest(
                                "GoogleBooks",
                                "volume-1",
                                "Dune",
                                null,
                                ["Frank Herbert"],
                                null,
                                "1965",
                                "en",
                                null,
                                null,
                                null,
                                null,
                                null),
                            new BookMetadataImportCandidateRequest(
                                "OpenLibrary",
                                "work-2",
                                "Dune Messiah",
                                null,
                                ["Frank Herbert"],
                                null,
                                "1969",
                                "en",
                                null,
                                null,
                                null,
                                null,
                                null),
                        ])]));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var session = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(session);
        var candidate = Assert.Single(session!.Candidates);

        var selectedMetadata = new BookMetadataImportCandidateRequest(
            "OpenLibrary",
            "work-2",
            "Dune Messiah",
            null,
            ["Frank Herbert"],
            null,
            "1969",
            "en",
            null,
            null,
            null,
            null,
            null);
        var purchaseResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                SelectedMetadata: selectedMetadata,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork));

        Assert.Equal(HttpStatusCode.Created, purchaseResponse.StatusCode);

        var reload = await client.GetAsync($"/api/family/current/scan-sessions/{session.ScanSessionId}");
        Assert.Equal(HttpStatusCode.OK, reload.StatusCode);
        var reloaded = await reload.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(reloaded);
        var reloadedCandidate = Assert.Single(reloaded!.Candidates);
        Assert.Equal("Dune Messiah", reloadedCandidate.DisplayTitle);
        Assert.Equal("Frank Herbert", reloadedCandidate.Author);
        var persistedMetadata = Assert.Single(reloadedCandidate.MetadataSnapshot!.Matches);
        Assert.Equal("work-2", persistedMetadata.SourceId);
        Assert.Equal("Dune Messiah", persistedMetadata.Title);
    }

    [Fact]
    public async Task Purchase_persists_manual_title_and_author_for_session_reload()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Manual Snapshot Family", "Purchaser");

        var session = await CreateSessionAsync(client, "Dune");
        var candidate = Assert.Single(session.Candidates);
        var purchaseResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune Messiah",
                ManualAuthor: "Frank Herbert"));

        Assert.Equal(HttpStatusCode.Created, purchaseResponse.StatusCode);

        var reload = await client.GetAsync($"/api/family/current/scan-sessions/{session.ScanSessionId}");
        Assert.Equal(HttpStatusCode.OK, reload.StatusCode);
        var reloaded = await reload.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(reloaded);
        var reloadedCandidate = Assert.Single(reloaded!.Candidates);
        Assert.Equal("Dune Messiah", reloadedCandidate.DisplayTitle);
        Assert.Equal("Frank Herbert", reloadedCandidate.Author);
        Assert.NotNull(reloadedCandidate.MetadataSnapshot);
        Assert.Empty(reloadedCandidate.MetadataSnapshot!.Matches);
    }

    [Fact]
    public async Task Purchase_does_not_load_every_edition_in_the_family_catalog()
    {
        var interceptor = new RecordingDbCommandInterceptor();
        await using var factory = await ApiFactory.CreateAsync(interceptor);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var login = await LoginAsync(client, "Purchase Query Family", "Purchaser");
        await using (var setupScope = factory.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<LibroryDbContext>();
            var family = await db.Families
                .Include(item => item.Members)
                .SingleAsync(item => item.Name == "Purchase Query Family");
            var work = BookWork.Create("Existing catalog work");
            var edition = work.AddEdition("9780000000002", "Paperback", 2026);
            work.AddEdition("9780000000003", "Hardcover", 2025);
            family.AddBookCopy(edition, family.Members.Single(item => item.Id == login.MemberId));
            db.BookWorks.Add(work);
            await db.SaveChangesAsync();
        }

        var session = await CreateSessionAsync(client, "Dune");
        var candidate = Assert.Single(session.Candidates);
        interceptor.Clear();

        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.DoesNotContain(
            interceptor.Commands,
            command => command.Contains("book_copies", StringComparison.OrdinalIgnoreCase)
                && command.Contains("book_works", StringComparison.OrdinalIgnoreCase)
                && command.Split("book_editions", StringSplitOptions.None).Length > 2);
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
    public async Task Purchase_rejects_oversized_selected_metadata_at_the_api_boundary()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Metadata Length Family", "Purchaser");

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
                    new string('x', 301),
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
    public async Task Purchase_rejects_overlong_manual_and_copy_fields_at_the_api_boundary()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Field Boundary Family", "Purchaser");

        var session = await CreateSessionAsync(client, "Dune");
        var candidate = Assert.Single(session.Candidates);
        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: new string('t', 301),
                ManualAuthor: new string('a', 301),
                Isbn: new string('9', 33),
                Format: new string('f', 65),
                Condition: new string('c', 201),
                PurchaseStore: new string('s', 201),
                ShelfLocation: new string('l', 201),
                IntakeNotes: new string('n', 4001),
                PurchasePrice: -1m,
                PublicationYear: 999));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Scan_session_rejects_invalid_nested_metadata_before_persisting_snapshot()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Scan Metadata Boundary Family", "Owner");

        var response = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf.jpg",
                Candidates:
                [
                    new CreateScanCandidateRequest(
                        "Dune",
                        "High",
                        MetadataMatches:
                        [
                            new BookMetadataImportCandidateRequest(
                                null!,
                                "volume-1",
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
                                null),
                        ]),
                ]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Scan_session_rejects_metadata_urls_with_non_http_schemes()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Scan Metadata Url Family", "Owner");

        var response = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf.jpg",
                Candidates:
                [
                    new CreateScanCandidateRequest(
                        "Dune",
                        "High",
                        MetadataMatches:
                        [
                            new BookMetadataImportCandidateRequest(
                                "GoogleBooks",
                                "volume-1",
                                "Dune",
                                null,
                                ["Frank Herbert"],
                                null,
                                null,
                                null,
                                null,
                                null,
                                null,
                                "javascript:alert(1)",
                                "javascript:alert(2)"),
                        ]),
                ]));

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
    public async Task Purchase_with_partial_version_metadata_remains_provisional()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Partial Version Family", "Purchaser");

        var session = await CreateSessionAsync(client, "Dune");
        var candidate = Assert.Single(session.Candidates);
        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune",
                PublicationYear: 2024));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var purchase = await response.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.NotNull(purchase);
        Assert.True(purchase!.IsProvisional);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
        var edition = await db.BookEditions.FindAsync(purchase.BookEditionId);
        Assert.NotNull(edition);
        Assert.True(edition!.IsProvisional);
        Assert.Equal(2024, edition.PublicationYear);
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
    public async Task Version_confirmation_checks_active_member_inside_the_serializable_transaction()
    {
        var interceptor = new ActiveMemberTransactionInterceptor();
        await using var factory = await ApiFactory.CreateAsync(interceptor);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Edition Authorization Transaction Family", "Purchaser");

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

        interceptor.Clear();
        var response = await client.PutAsJsonAsync(
            $"/api/family/current/book-editions/{purchase!.BookEditionId}/version",
            new UpdateBookEditionVersionRequest(null, "Paperback", 1965));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(IsolationLevel.Serializable, interceptor.ActiveMemberQueryIsolationLevel);
    }

    [Fact]
    public async Task Version_confirmation_requires_a_family_scoped_purchased_candidate_link()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Unlinked Provisional Edition Family", "Purchaser");

        Guid editionId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
            var family = await db.Families
                .Include(item => item.Members)
                .SingleAsync(item => item.Name == "Unlinked Provisional Edition Family");
            var work = BookWork.Create("Unlinked provisional");
            var edition = work.AddEdition();
            edition.IsProvisional = true;
            family.AddBookCopy(edition, family.Members.Single());
            db.BookWorks.Add(work);
            await db.SaveChangesAsync();
            editionId = edition.Id;
        }

        var response = await client.PutAsJsonAsync(
            $"/api/family/current/book-editions/{editionId}/version",
            new UpdateBookEditionVersionRequest("9780441013593", "Paperback", 1965));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirmed_edition_cannot_be_version_updated_again_through_the_family_route()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Confirmed Edition Protection Family", "Purchaser");

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

        var confirm = await client.PutAsJsonAsync(
            $"/api/family/current/book-editions/{purchase!.BookEditionId}/version",
            new UpdateBookEditionVersionRequest(null, "Paperback", 1965));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        var secondUpdate = await client.PutAsJsonAsync(
            $"/api/family/current/book-editions/{purchase.BookEditionId}/version",
            new UpdateBookEditionVersionRequest(null, "Hardcover", null));

        Assert.Equal(HttpStatusCode.NotFound, secondUpdate.StatusCode);
    }

    [Fact]
    public async Task Overlong_edition_version_fields_are_rejected_as_validation()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Edition Version Validation Family", "Purchaser");

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

        var response = await client.PutAsJsonAsync(
            $"/api/family/current/book-editions/{purchase!.BookEditionId}/version",
            new UpdateBookEditionVersionRequest(new string('9', 33), "Paperback", 1965));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Version_confirmation_returns_conflict_when_the_database_rejects_a_concurrent_update()
    {
        var interceptor = new SerializationFailureInterceptor();
        await using var factory = await ApiFactory.CreateAsync(interceptor);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Concurrent Edition Confirmation Family", "Purchaser");

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

        interceptor.Arm("book_editions");
        var response = await client.PutAsJsonAsync(
            $"/api/family/current/book-editions/{purchase!.BookEditionId}/version",
            new UpdateBookEditionVersionRequest("9780441013593", "Paperback", 1965));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Candidate_correction_returns_conflict_when_the_database_rejects_a_concurrent_update()
    {
        var interceptor = new SerializationFailureInterceptor();
        await using var factory = await ApiFactory.CreateAsync(interceptor);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Concurrent Candidate Correction Family", "Owner");

        var session = await CreateSessionAsync(client, "Dune");
        var candidate = Assert.Single(session.Candidates);
        interceptor.Arm("scan_candidates");

        var response = await client.PutAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}",
            new UpdateScanCandidateRequest("Dune revised", "High"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deactivated_member_cannot_confirm_a_purchased_provisional_edition()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var adminClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var memberClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        await LoginAsync(adminClient, "Active Edition Member Family", "Admin");
        var member = await LoginAsync(memberClient, "Active Edition Member Family", "Reader");

        var session = await CreateSessionAsync(adminClient, "Dune");
        var candidate = Assert.Single(session.Candidates);
        var purchaseResponse = await adminClient.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                member.MemberId,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune"));
        var purchase = await purchaseResponse.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.Equal(HttpStatusCode.Created, purchaseResponse.StatusCode);
        Assert.NotNull(purchase);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
            var reader = await db.Members.SingleAsync(item => item.Id == member.MemberId);
            reader.Deactivate();
            await db.SaveChangesAsync();
        }

        var response = await memberClient.PutAsJsonAsync(
            $"/api/family/current/book-editions/{purchase!.BookEditionId}/version",
            new UpdateBookEditionVersionRequest("9780441013593", "Paperback", 1965));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
    public async Task Purchase_returns_retryable_response_after_serializable_retries_are_exhausted()
    {
        var interceptor = new OneShotSerializationFailureInterceptor();
        await using var factory = await ApiFactory.CreateAsync(interceptor);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Retry Exhaustion Family", "Purchaser");

        var session = await CreateSessionAsync(client, "Dune");
        var candidate = Assert.Single(session.Candidates);
        interceptor.Arm(3);

        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.True(payload!["retryable"].GetBoolean());
        Assert.Equal(3, interceptor.InjectedFailures);
    }

    [Fact]
    public async Task Purchase_retries_after_a_purchase_request_unique_conflict()
    {
        var interceptor = new UniquePurchaseRequestFailureInterceptor();
        await using var factory = await ApiFactory.CreateAsync(interceptor);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Request Retry Family", "Purchaser");

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

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
    public async Task Purchase_rejects_existing_canonical_selection_without_a_matching_candidate_duplicate()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Canonical Selection Boundary Family", "Purchaser");

        Guid canonicalEditionId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
            var work = BookWork.Create("Canonical edition without a family copy");
            var edition = work.AddEdition("9780000000000", "Paperback", 2026);
            db.BookWorks.Add(work);
            await db.SaveChangesAsync();
            canonicalEditionId = edition.Id;
        }

        var newSession = await CreateSessionAsync(client, "Matilda");
        var newCandidate = Assert.Single(newSession.Candidates);
        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{newSession.ScanSessionId}/candidates/{newCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                newSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.ExistingEdition,
                DuplicateStatus: BookCopyDuplicateStatus.ConfirmedDuplicate,
                ExistingBookEditionId: canonicalEditionId,
                ManualTitle: "Matilda"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_rejects_an_invalid_purchase_time()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Purchase Time Validation Family", "Purchaser");

        var session = await CreateSessionAsync(client, "Matilda");
        var candidate = Assert.Single(session.Candidates);
        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Matilda",
                PurchasedAt: DateTimeOffset.MaxValue));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_rejects_canonical_ids_when_duplicate_resolution_is_not_specified()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Unspecified Resolution Family", "Purchaser");

        Guid canonicalEditionId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
            var work = BookWork.Create("Canonical target");
            var edition = work.AddEdition("9780000000001", "Paperback", 2026);
            db.BookWorks.Add(work);
            await db.SaveChangesAsync();
            canonicalEditionId = edition.Id;
        }

        var session = await CreateSessionAsync(client, "Matilda");
        var candidate = Assert.Single(session.Candidates);
        var response = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                ExistingBookEditionId: canonicalEditionId,
                ManualTitle: "Matilda"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Empty_version_confirmation_is_rejected_for_an_empty_provisional_edition()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Empty Version Confirmation Family", "Purchaser");

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

        var response = await client.PutAsJsonAsync(
            $"/api/family/current/book-editions/{purchase!.BookEditionId}/version",
            new UpdateBookEditionVersionRequest(null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task Correction_without_metadata_refresh_preserves_the_existing_metadata_snapshot()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Correction Snapshot Family", "Owner");

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
        var session = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(session);
        var candidate = Assert.Single(session!.Candidates);

        var correction = await client.PutAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}",
            new UpdateScanCandidateRequest("Charlotte's Web revised", "High"));

        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
        var corrected = await correction.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.Equal("volume-1", Assert.Single(corrected!.Candidates).MetadataSnapshot!.Matches.Single().SourceId);
    }

    [Fact]
    public async Task Duplicate_confirmation_accepts_a_match_when_the_recognized_title_differs_from_the_selected_metadata()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Resolved Duplicate Family", "Purchaser");

        var firstSession = await CreateSessionAsync(client, "Dune");
        var firstCandidate = Assert.Single(firstSession.Candidates);
        var firstPurchase = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{firstSession.ScanSessionId}/candidates/{firstCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                firstSession.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune",
                Isbn: "9780441013593"));
        Assert.Equal(HttpStatusCode.Created, firstPurchase.StatusCode);

        var secondCreate = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf.jpg",
                Candidates:
                [
                    new CreateScanCandidateRequest(
                        "Dune novel",
                        "High",
                        MetadataMatches:
                        [
                            new BookMetadataImportCandidateRequest(
                                "GoogleBooks",
                                "dune-provider",
                                "Dune",
                                "A Novel",
                                ["Frank Herbert"],
                                null,
                                "1965",
                                "en",
                                null,
                                null,
                                "9780441013593",
                                null,
                                null),
                        ])]));
        var secondSession = await secondCreate.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(secondSession);
        var secondCandidate = Assert.Single(secondSession!.Candidates);

        var warning = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{secondSession.ScanSessionId}/candidates/{secondCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                secondSession.TargetMemberId!.Value,
                SelectedMetadata: new BookMetadataImportCandidateRequest(
                    "GoogleBooks",
                    "dune-provider",
                    "Dune",
                    "A Novel",
                    ["Frank Herbert"],
                    null,
                    "1965",
                    "en",
                    null,
                    null,
                    "9780441013593",
                    null,
                    null),
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                Isbn: "9780441013593"));
        var matches = await warning.Content.ReadFromJsonAsync<DuplicateConfirmationResponse>();
        Assert.Equal(HttpStatusCode.Conflict, warning.StatusCode);
        var match = Assert.Single(matches!.Matches);

        var confirmed = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{secondSession.ScanSessionId}/candidates/{secondCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                secondSession.TargetMemberId!.Value,
                SelectedMetadata: new BookMetadataImportCandidateRequest(
                    "GoogleBooks",
                    "dune-provider",
                    "Dune",
                    "A Novel",
                    ["Frank Herbert"],
                    null,
                    "1965",
                    "en",
                    null,
                    null,
                    "9780441013593",
                    null,
                    null),
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.ExistingEdition,
                DuplicateStatus: BookCopyDuplicateStatus.ConfirmedDuplicate,
                ExistingBookEditionId: match.BookEditionId));

        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
    }

    [Fact]
    public async Task Correction_preserves_omitted_recognition_rank_and_accepts_explicit_zero()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Correction Rank Compatibility Family", "Owner");

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf.jpg",
                Candidates: [new CreateScanCandidateRequest("Dune", "High", RecognitionRank: 7)]));
        var session = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(session);
        var candidate = Assert.Single(session!.Candidates);
        var url = $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{candidate.Id}";

        var omittedRank = await client.PutAsJsonAsync(
            url,
            new UpdateScanCandidateRequest("Dune revised", "High"));
        Assert.Equal(HttpStatusCode.OK, omittedRank.StatusCode);
        var preserved = await omittedRank.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.Equal(7, Assert.Single(preserved!.Candidates).RecognitionRank);

        var explicitZero = await client.PutAsJsonAsync(
            url,
            new UpdateScanCandidateRequest("Dune revised again", "High", RecognitionRank: 0));
        Assert.Equal(HttpStatusCode.OK, explicitZero.StatusCode);
        var reset = await explicitZero.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.Equal(0, Assert.Single(reset!.Candidates).RecognitionRank);
    }

    [Fact]
    public async Task Correcting_another_candidate_preserves_purchased_response_data()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "Correction Purchase Response Family", "Owner");

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf.jpg",
                Candidates:
                [
                    new CreateScanCandidateRequest("Dune", "High"),
                    new CreateScanCandidateRequest("Matilda", "High"),
                ]));
        var session = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(session);
        var purchasedCandidate = session!.Candidates[0];
        var correctedCandidate = session.Candidates[1];

        var purchaseResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{purchasedCandidate.Id}/purchase",
            new ConfirmScanPurchaseRequest(
                Guid.NewGuid(),
                session.TargetMemberId!.Value,
                DuplicateResolution: Librory.Application.Intake.DuplicateResolution.NewWork,
                ManualTitle: "Dune"));
        var purchase = await purchaseResponse.Content.ReadFromJsonAsync<ScanPurchaseResponse>();
        Assert.Equal(HttpStatusCode.Created, purchaseResponse.StatusCode);
        Assert.NotNull(purchase);

        var correctionResponse = await client.PutAsJsonAsync(
            $"/api/family/current/scan-sessions/{session.ScanSessionId}/candidates/{correctedCandidate.Id}",
            new UpdateScanCandidateRequest("Matilda revised", "High", RecognitionEvidence: "Manual correction"));

        Assert.Equal(HttpStatusCode.OK, correctionResponse.StatusCode);
        var corrected = await correctionResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        var correctedPurchasedCandidate = corrected!.Candidates.Single(item => item.Id == purchasedCandidate.Id);
        Assert.NotNull(correctedPurchasedCandidate.Purchase);
        Assert.Equal(purchase!.BookEditionId, correctedPurchasedCandidate.Purchase!.BookEditionId);
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

        public void Arm(int failureCount = 1)
        {
            _remainingFailures = failureCount;
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
                if (Volatile.Read(ref _remainingFailures) == 0)
                {
                    Volatile.Write(ref _armed, 0);
                }
                throw new PostgresException("Serialization failure", "ERROR", "ERROR", "40001");
            }
        }
    }

    private sealed class RecordingDbCommandInterceptor : DbCommandInterceptor
    {
        private readonly List<string> _commands = [];

        public IReadOnlyList<string> Commands
        {
            get
            {
                lock (_commands)
                {
                    return _commands.ToArray();
                }
            }
        }

        public void Clear()
        {
            lock (_commands)
            {
                _commands.Clear();
            }
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Record(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        private void Record(DbCommand command)
        {
            lock (_commands)
            {
                _commands.Add(command.CommandText);
            }
        }
    }

    private sealed class ActiveMemberTransactionInterceptor : DbCommandInterceptor
    {
        public IsolationLevel? ActiveMemberQueryIsolationLevel { get; private set; }

        public void Clear()
        {
            ActiveMemberQueryIsolationLevel = null;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Record(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        private void Record(DbCommand command)
        {
            if (command.CommandText.Contains("FROM librory.members", StringComparison.OrdinalIgnoreCase))
            {
                ActiveMemberQueryIsolationLevel = command.Transaction?.IsolationLevel;
            }
        }
    }

    private sealed class SerializationFailureInterceptor : DbCommandInterceptor
    {
        private string? _table;

        public void Arm(string table)
        {
            _table = table;
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            ThrowIfArmed(command);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ThrowIfArmed(command);
            return ValueTask.FromResult(result);
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
            if (_table is not null
                && command.CommandText.Contains(_table, StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                _table = null;
                throw new PostgresException("Serialization failure", "ERROR", "ERROR", "40001");
            }
        }
    }

    private sealed class UniquePurchaseRequestFailureInterceptor : DbCommandInterceptor
    {
        private int _armed;

        public int InjectedFailures { get; private set; }

        public void Arm()
        {
            _armed = 1;
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            ThrowIfArmed(command);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ThrowIfArmed(command);
            return ValueTask.FromResult(result);
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
            if (Interlocked.Exchange(ref _armed, 0) == 1)
            {
                InjectedFailures++;
                throw new PostgresException(
                    "Purchase request id conflict",
                    "ERROR",
                    "ERROR",
                    "23505",
                    constraintName: "IX_scan_candidates_PurchaseRequestId");
            }
        }
    }
}
