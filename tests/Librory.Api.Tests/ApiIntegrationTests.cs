using System.Net.Http.Json;
using System.Text.Json;
using Librory.Api.Contracts;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Librory.Application.Wishlist;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Librory.Api.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task DevLogin_is_idempotent_for_same_family_and_member()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var request = new DevLoginRequest("Demo Family", "Test Admin", PreferredLanguage.English);

        var firstResponse = await client.PostAsJsonAsync("/dev/auth/login", request);
        await AssertSuccessAsync(firstResponse);

        var firstLogin = await firstResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(firstLogin);

        var secondResponse = await client.PostAsJsonAsync("/dev/auth/login", request);
        await AssertSuccessAsync(secondResponse);

        var secondLogin = await secondResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(secondLogin);

        Assert.Equal(firstLogin!.FamilyId, secondLogin!.FamilyId);
        Assert.Equal(firstLogin.MemberId, secondLogin.MemberId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();

        Assert.Equal(1, await db.Families.CountAsync());
        Assert.Equal(1, await db.Members.CountAsync());
    }

    [Fact]
    public async Task Current_family_endpoint_returns_counts_after_login()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginResponse = await client.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest("Counts Family", "Counts Admin", PreferredLanguage.English));

        await AssertSuccessAsync(loginResponse);

        var response = await client.GetAsync("/api/family/current");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CurrentFamilyResponse>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.MemberCount);
        Assert.Equal(0, payload.BookCount);
        Assert.Equal(0, payload.WishlistCount);
    }

    [Fact]
    public async Task Deactivated_admin_cannot_create_family_members_with_an_existing_cookie()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginResponse = await client.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest("Inactive Admin Family", "Inactive Admin", PreferredLanguage.English));
        await AssertSuccessAsync(loginResponse);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
            var member = await db.Members.SingleAsync();
            member.Deactivate();
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync(
            "/api/family/current/members",
            new CreateMemberRequest("Should not be created", PreferredLanguage.English));

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Bootstrap_creates_a_demo_family_and_is_idempotent()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var firstResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(firstResponse);

        var firstBootstrap = await firstResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(firstBootstrap);

        var secondResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(secondResponse);

        var secondBootstrap = await secondResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(secondBootstrap);

        Assert.Equal(firstBootstrap!.FamilyId, secondBootstrap!.FamilyId);
        Assert.Equal(firstBootstrap.MemberId, secondBootstrap.MemberId);
        Assert.Equal("Demo Family", firstBootstrap.FamilyName);
        Assert.Equal("Demo Admin", firstBootstrap.MemberDisplayName);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();

        Assert.Equal(1, await db.Families.CountAsync());
        Assert.Equal(1, await db.Members.CountAsync());
    }

    [Fact]
    public async Task Logout_clears_the_authentication_cookie()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginResponse = await client.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest("Logout Family", "Logout Admin", PreferredLanguage.English));

        await AssertSuccessAsync(loginResponse);

        var logoutResponse = await client.PostAsync("/dev/auth/logout", content: null);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var response = await client.GetAsync("/api/family/current");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_me_route_is_not_mapped()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Wishlist_is_paged()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        await client.PostAsJsonAsync("/api/family/current/wishlist", new CreateWishlistItemRequest("Item 1"));
        await client.PostAsJsonAsync("/api/family/current/wishlist", new CreateWishlistItemRequest("Item 2"));
        await client.PostAsJsonAsync("/api/family/current/wishlist", new CreateWishlistItemRequest("Item 3"));

        var response = await client.GetAsync("/api/family/current/wishlist?page=2&pageSize=2");
        await AssertSuccessAsync(response);

        var payload = await response.Content.ReadFromJsonAsync<WishlistPageResponse>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Page);
        Assert.Equal(2, payload.PageSize);
        Assert.Equal(3, payload.TotalCount);
        Assert.Single(payload.Items);
    }

    [Fact]
    public async Task Wishlist_item_created_location_can_be_fetched()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/wishlist",
            new CreateWishlistItemRequest("Item 1"));

        await AssertSuccessAsync(createResponse);

        Assert.NotNull(createResponse.Headers.Location);

        var getResponse = await client.GetAsync(createResponse.Headers.Location);
        await AssertSuccessAsync(getResponse);

        var payload = await getResponse.Content.ReadFromJsonAsync<WishlistItemDto>();
        Assert.NotNull(payload);
        Assert.Equal("Item 1", payload!.Title);
    }

    [Fact]
    public async Task Scan_session_can_be_created_and_read_back()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf-photo.jpg",
                3,
                [
                    new CreateScanCandidateRequest(
                        "Charlotte's Web",
                        "High",
                        "E. B. White",
                        0.92m,
                        false,
                        "Already owned by the family"),
                    new CreateScanCandidateRequest(
                        "Matilda",
                        "Medium",
                        "Roald Dahl",
                        0.78m)]));

        await AssertSuccessAsync(createResponse);

        var created = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);
        Assert.Equal("shelf-photo.jpg", created!.ShelfPhotoPath);
        Assert.Equal(2, created.Candidates.Count);

        var getResponse = await client.GetAsync($"/api/family/current/scan-sessions/{created.ScanSessionId}");
        await AssertSuccessAsync(getResponse);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.ScanSessionId, fetched!.ScanSessionId);
        Assert.Equal(created.ShelfPhotoPath, fetched.ShelfPhotoPath);
        Assert.Equal(created.Candidates.Count, fetched.Candidates.Count);
    }

    [Fact]
    public async Task Scan_session_candidate_can_be_corrected_in_place()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf-photo.jpg",
                3,
                [
                    new CreateScanCandidateRequest(
                        "Charlotte's Web",
                        "High",
                        "E. B. White",
                        0.92m,
                        false,
                        "Already owned by the family"),
                    new CreateScanCandidateRequest(
                        "Matilda",
                        "Medium",
                        "Roald Dahl",
                        0.78m)]));

        await AssertSuccessAsync(createResponse);

        var created = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);

        var candidateId = created!.Candidates[0].Id;
        var correctionResponse = await client.PutAsJsonAsync(
            $"/api/family/current/scan-sessions/{created.ScanSessionId}/candidates/{candidateId}",
            new UpdateScanCandidateRequest(
                "The Spider and the Pig",
                "Medium",
                "E. B. White",
                0.87m,
                false,
                "Recheck duplicate after correction"));

        await AssertSuccessAsync(correctionResponse);

        var corrected = await correctionResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(corrected);
        Assert.Equal(created.ScanSessionId, corrected!.ScanSessionId);
        Assert.Equal(2, corrected.Candidates.Count);

        var correctedCandidate = corrected.Candidates.Single(candidate => candidate.Id == candidateId);
        Assert.Equal("The Spider and the Pig", correctedCandidate.DisplayTitle);
        Assert.Equal("Medium", correctedCandidate.ConfidenceLabel);
        Assert.Equal("E. B. White", correctedCandidate.Author);
        Assert.Equal(0.87m, correctedCandidate.RecommendationScore);
        Assert.False(correctedCandidate.IsAlreadyOwned);
        Assert.Equal("Recheck duplicate after correction", correctedCandidate.DuplicateMessage);

        var untouchedCandidate = corrected.Candidates.Single(candidate => candidate.Id != candidateId);
        Assert.Equal("Matilda", untouchedCandidate.DisplayTitle);
        Assert.Equal("Medium", untouchedCandidate.ConfidenceLabel);
    }

    [Fact]
    public async Task Wishlist_page_validation_omits_empty_error_keys()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var response = await client.GetAsync("/api/family/current/wishlist?page=0&pageSize=0");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var errors = payload.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty("page", out var pageErrors));
        Assert.True(errors.TryGetProperty("pageSize", out var pageSizeErrors));
        Assert.Equal(JsonValueKind.Array, pageErrors.ValueKind);
        Assert.Equal(JsonValueKind.Array, pageSizeErrors.ValueKind);
        Assert.True(pageErrors.GetArrayLength() > 0);
        Assert.True(pageSizeErrors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Scan_session_candidate_can_be_promoted_into_a_new_book_work()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf-photo.jpg",
                3,
                [
                    new CreateScanCandidateRequest(
                        "The Test Title",
                        "High",
                        "E. B. White",
                        0.92m)]));

        await AssertSuccessAsync(createResponse);

        var created = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);

        var candidateId = created!.Candidates.Single().Id;
        var resolveResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{created.ScanSessionId}/candidates/{candidateId}/resolve",
            new ResolveScanCandidateRequest(
                "The Test Title",
                "E. B. White",
                "978-0-06-112495-2",
                "Hardcover",
                2006));

        await AssertSuccessAsync(resolveResponse);

        var resolved = await resolveResponse.Content.ReadFromJsonAsync<BookWorkResponse>();
        Assert.NotNull(resolved);
        Assert.NotEqual(Guid.Empty, resolved!.BookWorkId);
        Assert.Equal("The Test Title", resolved.Title);
        Assert.Equal("E. B. White", resolved.Author);
        Assert.Single(resolved.Editions);
        Assert.Equal("978-0-06-112495-2", resolved.Editions[0].Isbn);
        Assert.Equal("Hardcover", resolved.Editions[0].Format);
        Assert.Equal(2006, resolved.Editions[0].PublicationYear);

        var getResponse = await client.GetAsync($"/api/family/current/scan-sessions/{created.ScanSessionId}");
        await AssertSuccessAsync(getResponse);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(fetched);
        Assert.Empty(fetched!.Candidates);
    }

    [Fact]
    public async Task Scan_session_candidate_resolve_returns_not_found_for_missing_candidate()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf-photo.jpg",
                3,
                [
                    new CreateScanCandidateRequest(
                        "The Test Title",
                        "High",
                        "E. B. White",
                        0.92m)]));

        await AssertSuccessAsync(createResponse);

        var created = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);

        var resolveResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{created!.ScanSessionId}/candidates/{Guid.NewGuid()}/resolve",
            new ResolveScanCandidateRequest(
                "The Test Title",
                "E. B. White",
                "978-0-06-112495-2",
                "Hardcover",
                2006));

        Assert.Equal(System.Net.HttpStatusCode.NotFound, resolveResponse.StatusCode);
    }

    [Fact]
    public async Task Scan_session_candidate_resolve_returns_not_found_for_expired_session()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf-photo.jpg",
                3,
                [
                    new CreateScanCandidateRequest(
                        "The Test Title",
                        "High",
                        "E. B. White",
                        0.92m)]));

        await AssertSuccessAsync(createResponse);

        var created = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
            var session = await db.ScanSessions.SingleAsync(x => x.Id == created!.ScanSessionId);
            db.Entry(session).Property(x => x.ExpiresAt).CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
            await db.SaveChangesAsync();
        }

        var resolveResponse = await client.PostAsJsonAsync(
            $"/api/family/current/scan-sessions/{created!.ScanSessionId}/candidates/{created.Candidates[0].Id}/resolve",
            new ResolveScanCandidateRequest(
                "The Test Title",
                "E. B. White",
                "978-0-06-112495-2",
                "Hardcover",
                2006));

        Assert.Equal(System.Net.HttpStatusCode.NotFound, resolveResponse.StatusCode);
    }

    [Fact]
    public async Task Scan_session_candidate_can_be_discarded_from_the_session()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf-photo.jpg",
                3,
                [
                    new CreateScanCandidateRequest(
                        "The Test Title",
                        "High",
                        "E. B. White",
                        0.92m)]));

        await AssertSuccessAsync(createResponse);

        var created = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);

        var candidateId = created!.Candidates.Single().Id;
        var discardResponse = await client.DeleteAsync(
            $"/api/family/current/scan-sessions/{created.ScanSessionId}/candidates/{candidateId}");

        Assert.Equal(System.Net.HttpStatusCode.NoContent, discardResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/family/current/scan-sessions/{created.ScanSessionId}");
        await AssertSuccessAsync(getResponse);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(fetched);
        Assert.Empty(fetched!.Candidates);
    }

    [Fact]
    public async Task Scan_session_candidate_discard_returns_not_found_for_missing_candidate()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf-photo.jpg",
                3,
                [
                    new CreateScanCandidateRequest(
                        "The Test Title",
                        "High",
                        "E. B. White",
                        0.92m)]));

        await AssertSuccessAsync(createResponse);

        var created = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);

        var discardResponse = await client.DeleteAsync(
            $"/api/family/current/scan-sessions/{created!.ScanSessionId}/candidates/{Guid.NewGuid()}");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, discardResponse.StatusCode);
    }

    [Fact]
    public async Task Create_book_work_without_edition_leaves_editions_empty()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginResponse = await client.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest("Books Family", "Books Admin", PreferredLanguage.English));

        await AssertSuccessAsync(loginResponse);

        var response = await client.PostAsJsonAsync(
            "/api/book-works",
            new CreateBookWorkRequest("The Test Title"));

        await AssertSuccessAsync(response);

        var work = await response.Content.ReadFromJsonAsync<BookWorkResponse>();
        Assert.NotNull(work);
        Assert.Empty(work!.Editions);
    }

    [Fact]
    public async Task Manual_intake_can_create_a_book_copy_and_return_duplicate_summary()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(loginResponse);

        var login = await loginResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(login);

        var workResponse = await client.PostAsJsonAsync(
            "/api/book-works",
            new CreateBookWorkRequest(
                "Charlotte's Web",
                "E. B. White",
                "978-0-06-112495-2",
                "Hardcover",
                2006));

        await AssertSuccessAsync(workResponse);

        var work = await workResponse.Content.ReadFromJsonAsync<BookWorkResponse>();
        Assert.NotNull(work);
        Assert.Single(work!.Editions);

        var intakeResponse = await client.PostAsJsonAsync(
            "/api/family/current/book-copies",
            new CreateBookCopyRequest(
                work.Editions[0].BookEditionId,
                BookCopyDuplicateStatus.ConfirmedUnique,
                "Good",
                "Thrift Shop",
                12.5m,
                "Kids shelf",
                new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
                "First copy after review"));

        Assert.Equal(System.Net.HttpStatusCode.Created, intakeResponse.StatusCode);
        Assert.NotNull(intakeResponse.Headers.Location);

        var intake = await intakeResponse.Content.ReadFromJsonAsync<ManualBookIntakeResponse>();
        Assert.NotNull(intake);
        Assert.NotNull(intake!.Copy);
        Assert.Equal(work.Editions[0].BookEditionId, intake.Copy.BookEditionId);
        Assert.Equal(login.MemberId, intake.Copy.MemberId);
        Assert.Equal(login.FamilyId, intake.Copy.FamilyId);
        Assert.Equal(BookCopyDuplicateStatus.ConfirmedUnique, intake.Copy.DuplicateStatus);
        Assert.Equal("Good", intake.Copy.Condition);
        Assert.Equal("Thrift Shop", intake.Copy.PurchaseStore);
        Assert.Equal(12.5m, intake.Copy.PurchasePrice);
        Assert.Equal("Kids shelf", intake.Copy.ShelfLocation);
        Assert.Equal(new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero), intake.Copy.PurchasedAt);
        Assert.Equal("First copy after review", intake.Copy.IntakeNotes);
        Assert.False(intake.HasPotentialDuplicate);
        Assert.Null(intake.DuplicateWarning);

        var fetchedResponse = await client.GetAsync(intakeResponse.Headers.Location);
        await AssertSuccessAsync(fetchedResponse);

        var fetched = await fetchedResponse.Content.ReadFromJsonAsync<BookCopyResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(intake.Copy.BookCopyId, fetched!.BookCopyId);
        Assert.Equal(intake.Copy.BookEditionId, fetched.BookEditionId);
        Assert.Equal(intake.Copy.DuplicateStatus, fetched.DuplicateStatus);
        Assert.Equal(intake.Copy.Condition, fetched.Condition);
    }

    [Fact]
    public async Task Manual_intake_reports_duplicates_when_the_family_already_owns_the_title()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(loginResponse);

        var workResponse = await client.PostAsJsonAsync(
            "/api/book-works",
            new CreateBookWorkRequest(
                "Charlotte's Web",
                "E. B. White",
                "978-0-06-112495-2",
                "Hardcover",
                2006));

        await AssertSuccessAsync(workResponse);

        var work = await workResponse.Content.ReadFromJsonAsync<BookWorkResponse>();
        Assert.NotNull(work);

        var firstIntakeResponse = await client.PostAsJsonAsync(
            "/api/family/current/book-copies",
            new CreateBookCopyRequest(work!.Editions[0].BookEditionId));

        await AssertSuccessAsync(firstIntakeResponse);

        var secondIntakeResponse = await client.PostAsJsonAsync(
            "/api/family/current/book-copies",
            new CreateBookCopyRequest(work.Editions[0].BookEditionId));

        await AssertSuccessAsync(secondIntakeResponse);

        var secondIntake = await secondIntakeResponse.Content.ReadFromJsonAsync<ManualBookIntakeResponse>();
        Assert.NotNull(secondIntake);
        Assert.True(secondIntake!.HasPotentialDuplicate);
        Assert.Equal("Capture ISBN or barcode information to confirm the edition.", secondIntake.DuplicateWarning);
    }

    [Fact]
    public async Task Manual_intake_returns_not_found_for_missing_edition()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(loginResponse);

        var response = await client.PostAsJsonAsync(
            "/api/family/current/book-copies",
            new CreateBookCopyRequest(Guid.NewGuid()));

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Manual_intake_copy_is_not_visible_to_another_family()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var firstClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var firstLoginResponse = await firstClient.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(firstLoginResponse);

        var workResponse = await firstClient.PostAsJsonAsync(
            "/api/book-works",
            new CreateBookWorkRequest("Charlotte's Web", "E. B. White", "978-0-06-112495-2", "Hardcover", 2006));

        await AssertSuccessAsync(workResponse);

        var work = await workResponse.Content.ReadFromJsonAsync<BookWorkResponse>();
        Assert.NotNull(work);

        var intakeResponse = await firstClient.PostAsJsonAsync(
            "/api/family/current/book-copies",
            new CreateBookCopyRequest(work!.Editions[0].BookEditionId));

        await AssertSuccessAsync(intakeResponse);

        var created = await intakeResponse.Content.ReadFromJsonAsync<ManualBookIntakeResponse>();
        Assert.NotNull(created);

        using var secondClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var secondLoginResponse = await secondClient.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest("Other Family", "Other Admin", PreferredLanguage.English));

        await AssertSuccessAsync(secondLoginResponse);

        var getResponse = await secondClient.GetAsync($"/api/family/current/book-copies/{created!.Copy.BookCopyId}");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Recommendation_profile_can_be_created_read_back_and_partially_updated()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PutAsJsonAsync(
            "/api/family/current/recommendation-profile",
            new UpsertRecommendationProfileRequest(
                8,
                12,
                ["Roald Dahl"],
                ["Fantasy"],
                ["Reflective"]));

        await AssertSuccessAsync(createResponse);

        var created = await createResponse.Content.ReadFromJsonAsync<RecommendationProfileResponse>();
        Assert.NotNull(created);
        Assert.Equal(8, created!.MinimumAge);
        Assert.Equal(12, created.MaximumAge);
        Assert.Equal(["Roald Dahl"], created.FavoriteAuthors);
        Assert.Equal(["Fantasy"], created.FavoriteGenres);
        Assert.Equal(["Reflective"], created.FavoriteStyles);

        var getResponse = await client.GetAsync("/api/family/current/recommendation-profile");
        await AssertSuccessAsync(getResponse);

        var fetched = await getResponse.Content.ReadFromJsonAsync<RecommendationProfileResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.MemberId, fetched!.MemberId);
        Assert.Equal(created.MinimumAge, fetched.MinimumAge);
        Assert.Equal(created.MaximumAge, fetched.MaximumAge);
        Assert.Equal(created.FavoriteAuthors, fetched.FavoriteAuthors);
        Assert.Equal(created.FavoriteGenres, fetched.FavoriteGenres);
        Assert.Equal(created.FavoriteStyles, fetched.FavoriteStyles);

        var updateResponse = await client.PutAsJsonAsync(
            "/api/family/current/recommendation-profile",
            new UpsertRecommendationProfileRequest(
                5,
                null,
                null,
                ["Adventure", "adventure", " "],
                null));

        await AssertSuccessAsync(updateResponse);

        var updated = await updateResponse.Content.ReadFromJsonAsync<RecommendationProfileResponse>();
        Assert.NotNull(updated);
        Assert.Equal(5, updated!.MinimumAge);
        Assert.Equal(12, updated.MaximumAge);
        Assert.Equal(["Roald Dahl"], updated.FavoriteAuthors);
        Assert.Equal(["Adventure"], updated.FavoriteGenres);
        Assert.Equal(["Reflective"], updated.FavoriteStyles);

        var memberScopedGetResponse = await client.GetAsync($"/api/family/current/members/{created.MemberId}/recommendation-profile");
        await AssertSuccessAsync(memberScopedGetResponse);

        var memberScopedUpdateResponse = await client.PutAsJsonAsync(
            $"/api/family/current/members/{created.MemberId}/recommendation-profile",
            new UpsertRecommendationProfileRequest(9, 13));
        await AssertSuccessAsync(memberScopedUpdateResponse);

        var memberScopedUpdated = await memberScopedUpdateResponse.Content.ReadFromJsonAsync<RecommendationProfileResponse>();
        Assert.NotNull(memberScopedUpdated);
        Assert.Equal(9, memberScopedUpdated!.MinimumAge);
        Assert.Equal(13, memberScopedUpdated.MaximumAge);

        var nullNonNullableFieldsResponse = await client.PutAsJsonAsync(
            $"/api/family/current/members/{created.MemberId}/recommendation-profile",
            new { profileVisibility = (string?)null, useInFamilyRecommendations = (bool?)null });
        await AssertSuccessAsync(nullNonNullableFieldsResponse);
        var preserved = await nullNonNullableFieldsResponse.Content.ReadFromJsonAsync<RecommendationProfileResponse>();
        Assert.NotNull(preserved);
        Assert.Equal(ProfileVisibility.Family, preserved!.ProfileVisibility);
        Assert.True(preserved.UseInFamilyRecommendations);

        var memberListResponse = await client.GetAsync("/api/family/current/members");
        await AssertSuccessAsync(memberListResponse);
        var members = await memberListResponse.Content.ReadFromJsonAsync<FamilyMemberResponse[]>();
        Assert.NotNull(members);
        var currentMember = Assert.Single(members!, member => member.MemberId == created.MemberId);
        Assert.True(currentMember.HasRecommendationProfile);
        Assert.True(currentMember.CanUseForFamilyRecommendations);

        var clearResponse = await client.PutAsJsonAsync(
            $"/api/family/current/members/{created.MemberId}/recommendation-profile",
            new { minimumAge = (int?)null, favoriteGenres = (string[]?)null });
        await AssertSuccessAsync(clearResponse);
        var cleared = await clearResponse.Content.ReadFromJsonAsync<RecommendationProfileResponse>();
        Assert.NotNull(cleared);
        Assert.Null(cleared!.MinimumAge);
        Assert.Empty(cleared.FavoriteGenres);
        Assert.Equal(13, cleared.MaximumAge);
        Assert.Equal(["Roald Dahl"], cleared.FavoriteAuthors);
    }

    [Fact]
    public async Task Recommendation_profile_returns_bad_request_for_invalid_age_range()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var response = await client.PutAsJsonAsync(
            "/api/family/current/recommendation-profile",
            new UpsertRecommendationProfileRequest(12, 8));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Recommendation_profile_returns_bad_request_for_invalid_json_value_type()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        using var content = new StringContent(
            "{\"minimumAge\":\"not-a-number\"}",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PutAsync("/api/family/current/recommendation-profile", content);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_manage_another_member_profile_but_families_cannot_cross_read_it()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var firstClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var secondClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        await AssertSuccessAsync(await firstClient.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest("Profile Family One", "Parent", PreferredLanguage.English)));

        var memberResponse = await firstClient.PostAsJsonAsync(
            "/api/family/current/members",
            new CreateMemberRequest("Child", PreferredLanguage.Chinese));
        await AssertSuccessAsync(memberResponse);
        var child = await memberResponse.Content.ReadFromJsonAsync<FamilyMemberResponse>();
        Assert.NotNull(child);

        var childProfileResponse = await firstClient.PutAsJsonAsync(
            $"/api/family/current/members/{child!.MemberId}/recommendation-profile",
            new UpsertRecommendationProfileRequest(10, 16, preferredBookLanguages: [PreferredLanguage.English]));
        await AssertSuccessAsync(childProfileResponse);

        await AssertSuccessAsync(await secondClient.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest("Profile Family Two", "Other Parent", PreferredLanguage.English)));

        var foreignReadResponse = await secondClient.GetAsync(
            $"/api/family/current/members/{child.MemberId}/recommendation-profile");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, foreignReadResponse.StatusCode);
    }

    private static async Task AssertSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"Expected success but received {(int)response.StatusCode} {response.StatusCode}.\n{body}");
    }
}
