using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace Vector.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for data correction submission workflow.
/// Validates the full flow: create submission -> submit correction -> verify correction persisted.
/// Uses shared SQLite in-memory database via configuration.
/// </summary>
public class DataCorrectionEndToEndTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private SqliteConnection _keepAliveConnection = null!;
    private readonly string _dbName = $"CorrectionTest_{Guid.NewGuid():N}";

    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public DataCorrectionEndToEndTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        var connectionString = $"Data Source={_dbName};Mode=Memory;Cache=Shared";
        _keepAliveConnection = new SqliteConnection(connectionString);
        await _keepAliveConnection.OpenAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
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

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "DataCorrection")]
    public async Task DataCorrectionWorkflow_SubmitAndRetrieve_WorksCorrectly()
    {
        var submissionId = await CreateSubmissionViaApi("Acme Corporation");
        _output.WriteLine($"Created submission: {submissionId}");

        var correctionRequest = new
        {
            Type = "InsuredInformation",
            FieldName = "InsuredName",
            CurrentValue = "Acme Corporation",
            ProposedValue = "ACME Corp International",
            Justification = "Legal name changed per latest filing"
        };

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/v1/submissions/{submissionId}/corrections", correctionRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        _output.WriteLine("Data correction submitted successfully");

        var getResponse = await _client.GetAsync(
            $"/api/v1/submissions/{submissionId}/corrections");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var corrections = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var correctionArray = corrections.EnumerateArray().ToList();
        correctionArray.Should().HaveCount(1);

        var correction = correctionArray[0];
        correction.GetProperty("fieldName").GetString().Should().Be("InsuredName");
        correction.GetProperty("proposedValue").GetString().Should().Be("ACME Corp International");
        correction.GetProperty("justification").GetString().Should().Be("Legal name changed per latest filing");

        _output.WriteLine("Data correction retrieved and verified successfully");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "DataCorrection")]
    public async Task DataCorrectionWorkflow_MultipleCorrections_AllPersisted()
    {
        var submissionId = await CreateSubmissionViaApi("Beta Industries LLC");

        var corrections = new[]
        {
            new
            {
                Type = "InsuredInformation",
                FieldName = "InsuredName",
                CurrentValue = "Beta Industries LLC",
                ProposedValue = "Beta Industries Inc.",
                Justification = "Corporate restructuring"
            },
            new
            {
                Type = "CoverageDetails",
                FieldName = "RequestedLimit",
                CurrentValue = "1000000",
                ProposedValue = "2000000",
                Justification = "Updated limit per broker request"
            },
            new
            {
                Type = "LocationData",
                FieldName = "PrimaryState",
                CurrentValue = "CA",
                ProposedValue = "TX",
                Justification = "Headquarters relocated to Texas"
            }
        };

        foreach (var correction in corrections)
        {
            var response = await _client.PostAsJsonAsync(
                $"/api/v1/submissions/{submissionId}/corrections", correction);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var getResponse = await _client.GetAsync(
            $"/api/v1/submissions/{submissionId}/corrections");
        var result = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var correctionArray = result.EnumerateArray().ToList();

        correctionArray.Should().HaveCount(3);

        _output.WriteLine($"All {corrections.Length} corrections persisted and verified");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "DataCorrection")]
    public async Task DataCorrection_ForNonExistentSubmission_ReturnsNotFound()
    {
        var fakeSubmissionId = Guid.NewGuid();

        var correctionRequest = new
        {
            Type = "InsuredInformation",
            FieldName = "InsuredName",
            CurrentValue = "Old Name",
            ProposedValue = "New Name",
            Justification = "Name correction"
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/submissions/{fakeSubmissionId}/corrections", correctionRequest);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _output.WriteLine("Correctly returned 404 for non-existent submission");
    }

    private async Task<Guid> CreateSubmissionViaApi(string insuredName)
    {
        var request = new
        {
            TenantId = TenantId,
            InsuredName = insuredName
        };

        var response = await _client.PostAsJsonAsync("/api/v1/submissions", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
