using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vector.Infrastructure.Persistence;
using Xunit.Abstractions;

namespace Vector.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for admin portal workflows covering Guidelines, RoutingRules, and Pairings
/// through the HTTP API layer. Uses shared SQLite in-memory database via configuration to avoid
/// EF Core InMemory provider limitations with aggregate child entities.
/// </summary>
public class AdminPortalEndToEndTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private SqliteConnection _keepAliveConnection = null!;
    private readonly string _dbName = $"AdminTest_{Guid.NewGuid():N}";

    public AdminPortalEndToEndTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        // Use shared SQLite in-memory database (persists across connections with same name)
        var connectionString = $"Data Source={_dbName};Mode=Memory;Cache=Shared";
        _keepAliveConnection = new SqliteConnection(connectionString);
        await _keepAliveConnection.OpenAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Use SQLite via configuration - AddInfrastructure picks this up directly
                builder.UseSetting("ConnectionStrings:Sqlite", connectionString);
                builder.UseSetting("UseMockServices", "true");
                builder.UseSetting("SeedDatabase", "false");
                builder.UseSetting("Authentication:DisableAuthentication", "true");
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _keepAliveConnection.DisposeAsync();
    }

    #region Guidelines E2E Tests

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "Guidelines")]
    public async Task GuidelinesCrud_FullLifecycle_WorksCorrectly()
    {
        // Create
        var createRequest = new
        {
            Name = "E2E Test Guideline",
            Description = "Created during E2E testing",
            ApplicableCoverageTypes = "GeneralLiability",
            ApplicableStates = "CA,TX,NY",
            ApplicableNAICSCodes = "54",
            EffectiveDate = DateTime.Today.ToString("yyyy-MM-dd"),
            ExpirationDate = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd")
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/guidelines", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var guidelineId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        guidelineId.Should().NotBeEmpty();
        _output.WriteLine($"Created guideline: {guidelineId}");

        // Get by ID
        var getResponse = await _client.GetAsync($"/api/v1/guidelines/{guidelineId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var guideline = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        guideline.GetProperty("name").GetString().Should().Be("E2E Test Guideline");

        // Update
        var updateRequest = new
        {
            Name = "Updated E2E Guideline",
            Description = "Updated description",
            ApplicableCoverageTypes = "PropertyDamage",
            ApplicableStates = "CA",
            ApplicableNAICSCodes = "44,45",
            EffectiveDate = DateTime.Today.ToString("yyyy-MM-dd"),
            ExpirationDate = DateTime.Today.AddYears(2).ToString("yyyy-MM-dd")
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/guidelines/{guidelineId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Add a rule (required before activation)
        await AddRuleToGuidelineViaApi(guidelineId, "Lifecycle Rule");

        // Activate (requires at least one rule)
        var activateResponse = await _client.PostAsync($"/api/v1/guidelines/{guidelineId}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify active status
        var activeResponse = await _client.GetAsync($"/api/v1/guidelines/{guidelineId}");
        var active = await activeResponse.Content.ReadFromJsonAsync<JsonElement>();
        active.GetProperty("status").GetString().Should().Be("Active");

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/guidelines/{guidelineId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Delete (soft delete / archive)
        var deleteResponse = await _client.DeleteAsync($"/api/v1/guidelines/{guidelineId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify archived status
        var archivedResponse = await _client.GetAsync($"/api/v1/guidelines/{guidelineId}");
        archivedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archived = await archivedResponse.Content.ReadFromJsonAsync<JsonElement>();
        archived.GetProperty("status").GetString().Should().Be("Archived");

        _output.WriteLine("Guidelines full lifecycle completed successfully");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "Guidelines")]
    public async Task GuidelinesList_WithStatusFilter_ReturnsFilteredResults()
    {
        var g1 = await CreateGuidelineViaApi("Active Guideline Filter");
        await CreateGuidelineViaApi("Draft Guideline Filter");

        // Add a rule before activating
        await AddRuleToGuidelineViaApi(g1, "Filter Test Rule");
        await _client.PostAsync($"/api/v1/guidelines/{g1}/activate", null);

        var activeResponse = await _client.GetAsync("/api/v1/guidelines?status=Active");
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeGuidelines = await activeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var activeArray = activeGuidelines.EnumerateArray().ToList();

        activeArray.Should().Contain(g =>
            g.GetProperty("id").GetGuid() == g1);

        _output.WriteLine($"Active guidelines count: {activeArray.Count}");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "Guidelines")]
    public async Task GuidelinesWithRules_AddAndRemoveRules_WorksCorrectly()
    {
        var guidelineId = await CreateGuidelineViaApi("Guideline With Rules");

        var ruleId = await AddRuleToGuidelineViaApi(guidelineId, "Revenue Check");

        // Verify rule was added
        var getResponse = await _client.GetAsync($"/api/v1/guidelines/{guidelineId}");
        var guideline = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var rules = guideline.GetProperty("rules").EnumerateArray().ToList();
        rules.Should().HaveCount(1);
        rules[0].GetProperty("name").GetString().Should().Be("Revenue Check");

        var actualRuleId = rules[0].GetProperty("id").GetGuid();

        // Remove the rule
        var removeResponse = await _client.DeleteAsync(
            $"/api/v1/guidelines/{guidelineId}/rules/{actualRuleId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify rule was removed
        var verifyResponse = await _client.GetAsync($"/api/v1/guidelines/{guidelineId}");
        var verifiedGuideline = await verifyResponse.Content.ReadFromJsonAsync<JsonElement>();
        verifiedGuideline.GetProperty("rules").EnumerateArray().ToList().Should().BeEmpty();

        _output.WriteLine("Guidelines rule management completed successfully");
    }

    #endregion

    #region Routing Rules E2E Tests

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "RoutingRules")]
    public async Task RoutingRulesCrud_FullLifecycle_WorksCorrectly()
    {
        var createRequest = new
        {
            Name = "E2E Test Rule",
            Description = "Route high-value accounts",
            Strategy = "Direct",
            Priority = 80,
            TargetUnderwriterId = Guid.NewGuid(),
            TargetUnderwriterName = "John Smith",
            TargetTeamId = (Guid?)null,
            TargetTeamName = (string?)null
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/routing-rules", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var ruleId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        ruleId.Should().NotBeEmpty();
        _output.WriteLine($"Created routing rule: {ruleId}");

        // Get by ID
        var getResponse = await _client.GetAsync($"/api/v1/routing-rules/{ruleId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rule = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        rule.GetProperty("name").GetString().Should().Be("E2E Test Rule");
        rule.GetProperty("priority").GetInt32().Should().Be(80);

        // Update
        var updateRequest = new
        {
            Name = "Updated E2E Rule",
            Description = "Updated routing rule",
            Strategy = "RoundRobin",
            Priority = 60,
            TargetUnderwriterId = (Guid?)null,
            TargetUnderwriterName = (string?)null,
            TargetTeamId = Guid.NewGuid(),
            TargetTeamName = "Team Alpha"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/routing-rules/{ruleId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Activate
        var activateResponse = await _client.PostAsync($"/api/v1/routing-rules/{ruleId}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/routing-rules/{ruleId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Delete (soft delete / archive)
        var deleteResponse = await _client.DeleteAsync($"/api/v1/routing-rules/{ruleId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify archived status (soft delete means still accessible)
        var archivedResponse = await _client.GetAsync($"/api/v1/routing-rules/{ruleId}");
        archivedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archived = await archivedResponse.Content.ReadFromJsonAsync<JsonElement>();
        archived.GetProperty("status").GetString().Should().Be("Archived");

        _output.WriteLine("Routing rules full lifecycle completed successfully");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "RoutingRules")]
    public async Task RoutingRulesList_WithStatusFilter_ReturnsFilteredResults()
    {
        var r1 = await CreateRoutingRuleViaApi("Active Rule Filter", 90);
        await CreateRoutingRuleViaApi("Draft Rule Filter", 50);

        await _client.PostAsync($"/api/v1/routing-rules/{r1}/activate", null);

        var activeResponse = await _client.GetAsync("/api/v1/routing-rules?status=Active");
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rules = await activeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ruleArray = rules.EnumerateArray().ToList();

        ruleArray.Should().Contain(r =>
            r.GetProperty("id").GetGuid() == r1);

        _output.WriteLine($"Active routing rules count: {ruleArray.Count}");
    }

    #endregion

    #region Pairings E2E Tests

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "Pairings")]
    public async Task PairingsCrud_FullLifecycle_WorksCorrectly()
    {
        var createRequest = new
        {
            ProducerId = Guid.NewGuid(),
            ProducerName = "Test Producer Agency",
            UnderwriterId = Guid.NewGuid(),
            UnderwriterName = "Jane Underwriter",
            Priority = 75,
            EffectiveFrom = DateTime.Today.ToString("yyyy-MM-dd"),
            EffectiveUntil = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd"),
            CoverageTypes = new[] { "GeneralLiability", "PropertyDamage" }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/pairings", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var pairingId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        pairingId.Should().NotBeEmpty();
        _output.WriteLine($"Created pairing: {pairingId}");

        // Get by ID
        var getResponse = await _client.GetAsync($"/api/v1/pairings/{pairingId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pairing = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        pairing.GetProperty("producerName").GetString().Should().Be("Test Producer Agency");
        pairing.GetProperty("priority").GetInt32().Should().Be(75);

        // Update
        var updateRequest = new
        {
            Priority = 90,
            EffectiveFrom = DateTime.Today.ToString("yyyy-MM-dd"),
            EffectiveUntil = DateTime.Today.AddYears(2).ToString("yyyy-MM-dd"),
            CoverageTypes = new[] { "GeneralLiability" }
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/pairings/{pairingId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Activate
        var activateResponse = await _client.PostAsync($"/api/v1/pairings/{pairingId}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Deactivate
        var deactivateResponse = await _client.PostAsync($"/api/v1/pairings/{pairingId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Delete (hard delete for pairings)
        var deleteResponse = await _client.DeleteAsync($"/api/v1/pairings/{pairingId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted (hard delete returns 404)
        var deletedResponse = await _client.GetAsync($"/api/v1/pairings/{pairingId}");
        deletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _output.WriteLine("Pairings full lifecycle completed successfully");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "Pairings")]
    public async Task PairingsList_WithActiveFilter_ReturnsOnlyActive()
    {
        var p1 = await CreatePairingViaApi("Active Producer Filter", "Active UW Filter");
        await CreatePairingViaApi("Inactive Producer Filter", "Inactive UW Filter");

        await _client.PostAsync($"/api/v1/pairings/{p1}/activate", null);

        var activeResponse = await _client.GetAsync("/api/v1/pairings?activeOnly=true");
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pairings = await activeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var pairingArray = pairings.EnumerateArray().ToList();

        pairingArray.Should().Contain(p =>
            p.GetProperty("id").GetGuid() == p1);

        _output.WriteLine($"Active pairings count: {pairingArray.Count}");
    }

    #endregion

    #region Helper Methods

    private async Task<Guid> CreateGuidelineViaApi(string name)
    {
        var request = new
        {
            Name = name,
            Description = $"Test guideline: {name}",
            ApplicableCoverageTypes = "GeneralLiability",
            ApplicableStates = "CA",
            ApplicableNAICSCodes = "54",
            EffectiveDate = DateTime.Today.ToString("yyyy-MM-dd"),
            ExpirationDate = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd")
        };

        var response = await _client.PostAsJsonAsync("/api/v1/guidelines", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> AddRuleToGuidelineViaApi(Guid guidelineId, string ruleName)
    {
        var ruleRequest = new
        {
            Name = ruleName,
            Description = $"Test rule: {ruleName}",
            Type = "Eligibility",
            Action = "Decline",
            Priority = 100,
            ScoreAdjustment = (int?)null,
            PricingModifier = (decimal?)null,
            Message = "Test rule message"
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/guidelines/{guidelineId}/rules", ruleRequest);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"AddRule failed: {response.StatusCode} - {body}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreateRoutingRuleViaApi(string name, int priority)
    {
        var request = new
        {
            Name = name,
            Description = $"Test rule: {name}",
            Strategy = "Direct",
            Priority = priority,
            TargetUnderwriterId = Guid.NewGuid(),
            TargetUnderwriterName = "Test UW",
            TargetTeamId = (Guid?)null,
            TargetTeamName = (string?)null
        };

        var response = await _client.PostAsJsonAsync("/api/v1/routing-rules", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreatePairingViaApi(string producerName, string underwriterName)
    {
        var request = new
        {
            ProducerId = Guid.NewGuid(),
            ProducerName = producerName,
            UnderwriterId = Guid.NewGuid(),
            UnderwriterName = underwriterName,
            Priority = 50,
            EffectiveFrom = DateTime.Today.ToString("yyyy-MM-dd"),
            EffectiveUntil = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd"),
            CoverageTypes = Array.Empty<string>()
        };

        var response = await _client.PostAsJsonAsync("/api/v1/pairings", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    #endregion
}
