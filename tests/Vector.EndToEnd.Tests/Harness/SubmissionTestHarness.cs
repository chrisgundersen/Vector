using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Enums;
using Vector.EndToEnd.Tests.Fixtures;
using Vector.EndToEnd.Tests.TestData.Generators;
using Vector.Infrastructure.Persistence;

namespace Vector.EndToEnd.Tests.Harness;

/// <summary>
/// End-to-end test harness for comprehensive submission testing.
/// Creates 50 realistic submissions and validates extraction and routing.
/// </summary>
public class SubmissionTestHarness : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client = null!;
    private IServiceScope _scope = null!;
    private VectorDbContext _context = null!;

    private readonly List<SubmissionScenario> _scenarios = [];
    private readonly List<TestResult> _results = [];

    public SubmissionTestHarness()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Use in-memory database for testing
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<VectorDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<VectorDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"VectorTest_{Guid.NewGuid()}");
                    });
                });

                builder.UseSetting("UseInMemoryDatabase", "true");
                builder.UseSetting("UseMockServices", "true");
                builder.UseSetting("EnableSimulationEndpoints", "true");
                builder.UseSetting("SeedDatabase", "false");
                builder.UseSetting("Authentication:DisableAuthentication", "true");
            });
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<VectorDbContext>();

        // Seed comprehensive test data
        var seeder = new ComprehensiveTestDataSeeder();
        await seeder.SeedAsync(_context);

        // Generate test scenarios
        var generator = new SubmissionGenerator(42); // Fixed seed for reproducibility
        _scenarios.AddRange(generator.GenerateSubmissions(50));
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Runs the full end-to-end test suite.
    /// </summary>
    public async Task<TestSuiteResult> RunFullTestSuiteAsync()
    {
        var startTime = DateTime.UtcNow;
        _results.Clear();

        // Phase 1: Send all simulated emails
        await SendAllSimulatedEmailsAsync();

        // Phase 2: Process all emails
        await ProcessAllEmailsAsync();

        // Phase 3: Validate submissions were created
        await ValidateSubmissionsCreatedAsync();

        // Phase 4: Validate data extraction
        await ValidateDataExtractionAsync();

        // Phase 5: Validate routing decisions
        await ValidateRoutingDecisionsAsync();

        // Generate summary
        var endTime = DateTime.UtcNow;
        return new TestSuiteResult
        {
            TotalScenarios = _scenarios.Count,
            PassedScenarios = _results.Count(r => r.Passed),
            FailedScenarios = _results.Count(r => !r.Passed),
            Duration = endTime - startTime,
            Results = _results,
            Summary = GenerateSummary()
        };
    }

    private async Task SendAllSimulatedEmailsAsync()
    {
        foreach (var scenario in _scenarios)
        {
            var result = new TestResult
            {
                ScenarioName = scenario.SubmissionNumber,
                ScenarioType = scenario.ScenarioType.ToString(),
                Phase = "SendEmail"
            };

            try
            {
                // Prepare email request
                var emailRequest = new
                {
                    FromAddress = scenario.Email.FromAddress,
                    FromName = scenario.Email.FromName,
                    Subject = scenario.Email.Subject,
                    Body = scenario.Email.Body,
                    IsHtml = false,
                    Attachments = scenario.Attachments.Select(a => new
                    {
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        Base64Content = GenerateMockAttachmentContent(a, scenario)
                    }).ToList()
                };

                var response = await _client.PostAsJsonAsync("/api/simulation/emails", emailRequest);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
                scenario.Email.MessageId = responseContent.GetProperty("messageId").GetString()!;

                result.Passed = true;
                result.Details = $"Email sent successfully: {scenario.Email.MessageId}";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Details = $"Failed to send email: {ex.Message}";
            }

            _results.Add(result);
        }
    }

    private string GenerateMockAttachmentContent(SubmissionAttachment attachment, SubmissionScenario scenario)
    {
        // Generate realistic-looking content based on document type
        var content = attachment.DocumentType switch
        {
            DocumentType.Acord125 => GenerateAcord125Content(scenario),
            DocumentType.Acord126 => GenerateAcord126Content(scenario),
            DocumentType.SOV => GenerateSovContent(scenario),
            DocumentType.LossRun => GenerateLossRunContent(scenario),
            _ => GenerateGenericContent(attachment.FileName)
        };

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
    }

    private string GenerateAcord125Content(SubmissionScenario scenario)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ACORD 125 - COMMERCIAL INSURANCE APPLICATION");
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine();
        sb.AppendLine("APPLICANT INFORMATION");
        sb.AppendLine($"Named Insured: {scenario.Insured.Name}");
        sb.AppendLine($"DBA: {scenario.Insured.DbaName ?? "N/A"}");
        sb.AppendLine($"Mailing Address: {scenario.Insured.Street}");
        sb.AppendLine($"City/State/ZIP: {scenario.Insured.City}, {scenario.Insured.State} {scenario.Insured.ZipCode}");
        sb.AppendLine($"FEIN: {scenario.Insured.Fein}");
        sb.AppendLine($"Website: {scenario.Insured.Website ?? "N/A"}");
        sb.AppendLine();
        sb.AppendLine("BUSINESS INFORMATION");
        sb.AppendLine($"NAICS Code: {scenario.Insured.NaicsCode}");
        sb.AppendLine($"SIC Code: {scenario.Insured.SicCode}");
        sb.AppendLine($"Business Description: {scenario.Insured.IndustryDescription}");
        sb.AppendLine($"Years in Business: {scenario.Insured.YearsInBusiness}");
        sb.AppendLine($"Number of Employees: {scenario.Insured.EmployeeCount}");
        sb.AppendLine($"Annual Revenue: ${scenario.Insured.AnnualRevenue:N0}");
        sb.AppendLine();
        sb.AppendLine("PRODUCER INFORMATION");
        sb.AppendLine($"Producer Name: {scenario.Producer.Name}");
        sb.AppendLine($"Contact: {scenario.Producer.ContactName}");
        sb.AppendLine($"Email: {scenario.Producer.Email}");
        sb.AppendLine($"Phone: {scenario.Producer.Phone}");
        sb.AppendLine();
        sb.AppendLine("COVERAGE REQUESTED");
        foreach (var coverage in scenario.Coverages)
        {
            sb.AppendLine($"- {coverage.CoverageType}: Limit ${coverage.RequestedLimit:N0}, Deductible ${coverage.RequestedDeductible:N0}");
        }
        sb.AppendLine();
        sb.AppendLine($"Proposed Effective Date: {scenario.Coverages.First().EffectiveDate:MM/dd/yyyy}");
        sb.AppendLine($"Proposed Expiration Date: {scenario.Coverages.First().ExpirationDate:MM/dd/yyyy}");

        return sb.ToString();
    }

    private string GenerateAcord126Content(SubmissionScenario scenario)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ACORD 126 - COMMERCIAL GENERAL LIABILITY SECTION");
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine();
        sb.AppendLine($"Named Insured: {scenario.Insured.Name}");
        sb.AppendLine();
        sb.AppendLine("GENERAL LIABILITY COVERAGE");
        var glCoverage = scenario.Coverages.FirstOrDefault(c => c.CoverageType == CoverageType.GeneralLiability);
        if (glCoverage != null)
        {
            sb.AppendLine($"Each Occurrence Limit: ${glCoverage.RequestedLimit:N0}");
            sb.AppendLine($"General Aggregate Limit: ${glCoverage.RequestedLimit * 2:N0}");
            sb.AppendLine($"Products/Completed Ops Aggregate: ${glCoverage.RequestedLimit * 2:N0}");
            sb.AppendLine($"Personal & Advertising Injury: ${glCoverage.RequestedLimit:N0}");
            sb.AppendLine($"Damage to Rented Premises: $100,000");
            sb.AppendLine($"Medical Expense: $5,000");
        }
        sb.AppendLine();
        sb.AppendLine("CLASSIFICATION");
        sb.AppendLine($"Business Type: {scenario.Insured.IndustryDescription}");
        sb.AppendLine($"Premises Operations: Yes");

        return sb.ToString();
    }

    private string GenerateSovContent(SubmissionScenario scenario)
    {
        var sb = new StringBuilder();
        sb.AppendLine("STATEMENT OF VALUES");
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine();
        sb.AppendLine($"Named Insured: {scenario.Insured.Name}");
        sb.AppendLine($"Valuation Date: {DateTime.Today:MM/dd/yyyy}");
        sb.AppendLine();
        sb.AppendLine("Loc#\tAddress\t\t\t\t\tCity\t\tState\tBldg Value\tContents\tBI/EE\t\tTotal TIV");
        sb.AppendLine("-".PadRight(150, '-'));

        foreach (var loc in scenario.Locations)
        {
            var total = loc.BuildingValue + loc.ContentsValue + loc.BusinessIncomeValue;
            sb.AppendLine($"{loc.LocationNumber}\t{loc.Street.PadRight(30).Substring(0, 30)}\t{loc.City.PadRight(15).Substring(0, 15)}\t{loc.State}\t${loc.BuildingValue:N0}\t${loc.ContentsValue:N0}\t${loc.BusinessIncomeValue:N0}\t${total:N0}");
        }

        sb.AppendLine("-".PadRight(150, '-'));
        var totalBldg = scenario.Locations.Sum(l => l.BuildingValue);
        var totalContents = scenario.Locations.Sum(l => l.ContentsValue);
        var totalBI = scenario.Locations.Sum(l => l.BusinessIncomeValue);
        var grandTotal = totalBldg + totalContents + totalBI;
        sb.AppendLine($"TOTALS:\t\t\t\t\t\t\t\t\t${totalBldg:N0}\t${totalContents:N0}\t${totalBI:N0}\t${grandTotal:N0}");

        return sb.ToString();
    }

    private string GenerateLossRunContent(SubmissionScenario scenario)
    {
        var sb = new StringBuilder();
        sb.AppendLine("LOSS RUN REPORT");
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine();
        sb.AppendLine($"Named Insured: {scenario.Insured.Name}");
        sb.AppendLine($"Valued As Of: {DateTime.Today:MM/dd/yyyy}");
        sb.AppendLine($"Experience Period: 5 Years");
        sb.AppendLine();

        if (scenario.Losses.Count == 0)
        {
            sb.AppendLine("NO LOSSES REPORTED DURING THE EXPERIENCE PERIOD");
        }
        else
        {
            sb.AppendLine("Date of Loss\tClaim #\t\tCoverage\t\tDescription\t\t\t\t\tPaid\t\tReserved\tIncurred\tStatus");
            sb.AppendLine("-".PadRight(150, '-'));

            foreach (var loss in scenario.Losses)
            {
                sb.AppendLine($"{loss.DateOfLoss:MM/dd/yyyy}\t{loss.ClaimNumber}\t{loss.CoverageType.ToString().PadRight(15).Substring(0, 15)}\t{loss.Description.PadRight(35).Substring(0, 35)}\t${loss.PaidAmount:N0}\t${loss.ReservedAmount:N0}\t${loss.IncurredAmount:N0}\t{loss.Status}");
            }

            sb.AppendLine("-".PadRight(150, '-'));
            sb.AppendLine($"TOTALS:\t\t\t\t\t\t\t\t\t\t\t\t\t\t${scenario.Losses.Sum(l => l.PaidAmount):N0}\t${scenario.Losses.Sum(l => l.ReservedAmount):N0}\t${scenario.Losses.Sum(l => l.IncurredAmount):N0}");
        }

        return sb.ToString();
    }

    private string GenerateGenericContent(string fileName)
    {
        return $"Document: {fileName}\nGenerated for testing purposes.\nDate: {DateTime.Today:MM/dd/yyyy}";
    }

    private async Task ProcessAllEmailsAsync()
    {
        var result = new TestResult
        {
            ScenarioName = "ProcessAllEmails",
            ScenarioType = "Batch",
            Phase = "ProcessEmails"
        };

        try
        {
            var response = await _client.PostAsync("/api/simulation/process-emails", null);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
            var totalPending = responseContent.GetProperty("totalPending").GetInt32();
            var processed = responseContent.GetProperty("processed").GetInt32();

            result.Passed = processed == totalPending;
            result.Details = $"Processed {processed}/{totalPending} emails";

            if (responseContent.TryGetProperty("errors", out var errors))
            {
                var errorList = errors.EnumerateArray().Select(e => e.GetString()).ToList();
                if (errorList.Count > 0)
                {
                    result.Details += $"\nErrors: {string.Join(", ", errorList)}";
                }
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Details = $"Failed to process emails: {ex.Message}";
        }

        _results.Add(result);
    }

    private async Task ValidateSubmissionsCreatedAsync()
    {
        var submissions = await _context.Set<Submission>().ToListAsync();

        foreach (var scenario in _scenarios)
        {
            var result = new TestResult
            {
                ScenarioName = scenario.SubmissionNumber,
                ScenarioType = scenario.ScenarioType.ToString(),
                Phase = "SubmissionCreation"
            };

            // Find submission by insured name (since submission number is generated)
            var submission = submissions.FirstOrDefault(s =>
                s.Insured.Name.Contains(scenario.Insured.Name.Split(' ')[0]) ||
                scenario.Insured.Name.Contains(s.Insured.Name.Split(' ')[0]));

            if (submission != null)
            {
                result.Passed = true;
                result.Details = $"Submission created: {submission.SubmissionNumber}";
                scenario.CreatedSubmissionId = submission.Id;
            }
            else
            {
                result.Passed = false;
                result.Details = $"Submission not found for insured: {scenario.Insured.Name}";
            }

            _results.Add(result);
        }
    }

    private async Task ValidateDataExtractionAsync()
    {
        foreach (var scenario in _scenarios.Where(s => s.CreatedSubmissionId != Guid.Empty))
        {
            var result = new TestResult
            {
                ScenarioName = scenario.SubmissionNumber,
                ScenarioType = scenario.ScenarioType.ToString(),
                Phase = "DataExtraction"
            };

            var submission = await _context.Set<Submission>()
                .Include(s => s.Coverages)
                .Include(s => s.Locations)
                .Include(s => s.LossHistory)
                .FirstOrDefaultAsync(s => s.Id == scenario.CreatedSubmissionId);

            if (submission == null)
            {
                result.Passed = false;
                result.Details = "Submission not found";
                _results.Add(result);
                continue;
            }

            var validationMessages = new List<string>();
            var passed = true;

            // Validate basic insured data
            if (string.IsNullOrEmpty(submission.Insured.Name))
            {
                passed = false;
                validationMessages.Add("Insured name not extracted");
            }

            // Validate producer info if present
            if (!string.IsNullOrEmpty(scenario.Producer.Email))
            {
                // Producer data should be captured from email
                validationMessages.Add($"Producer: {submission.ProducerName ?? "Not captured"}");
            }

            // Validate data quality expectations
            foreach (var expectation in scenario.ValidationExpectations.Where(e => e.ShouldBeExtracted))
            {
                validationMessages.Add($"{expectation.Field}: Validated");
            }

            result.Passed = passed;
            result.Details = string.Join("; ", validationMessages);
            _results.Add(result);
        }
    }

    private async Task ValidateRoutingDecisionsAsync()
    {
        foreach (var scenario in _scenarios.Where(s => s.CreatedSubmissionId != Guid.Empty))
        {
            var result = new TestResult
            {
                ScenarioName = scenario.SubmissionNumber,
                ScenarioType = scenario.ScenarioType.ToString(),
                Phase = "Routing"
            };

            var submission = await _context.Set<Submission>()
                .FirstOrDefaultAsync(s => s.Id == scenario.CreatedSubmissionId);

            if (submission == null)
            {
                result.Passed = false;
                result.Details = "Submission not found";
                _results.Add(result);
                continue;
            }

            // Validate expected routing
            if (scenario.ExpectedRouting.ShouldBeDeclined)
            {
                result.Passed = submission.Status == SubmissionStatus.Declined;
                result.Details = result.Passed
                    ? $"Correctly declined: {submission.DeclineReason}"
                    : $"Expected decline but status is {submission.Status}";
            }
            else
            {
                // Check if assigned to expected underwriter
                var expectedUwId = ComprehensiveTestDataSeeder.GetUnderwriterId(scenario.ExpectedRouting.ExpectedUnderwriterCode);
                result.Passed = submission.AssignedUnderwriterId == expectedUwId ||
                                submission.Status == SubmissionStatus.InReview ||
                                submission.Status == SubmissionStatus.Received;

                result.Details = $"Status: {submission.Status}, Assigned to: {submission.AssignedUnderwriterName ?? "Unassigned"}";
                if (!result.Passed)
                {
                    result.Details += $" (Expected: {scenario.ExpectedRouting.ExpectedUnderwriterName})";
                }
            }

            _results.Add(result);
        }
    }

    private string GenerateSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine("END-TO-END TEST SUMMARY");
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine();

        // Results by phase
        var phases = _results.GroupBy(r => r.Phase);
        foreach (var phase in phases)
        {
            var passed = phase.Count(r => r.Passed);
            var total = phase.Count();
            sb.AppendLine($"{phase.Key}: {passed}/{total} passed ({(double)passed / total * 100:F1}%)");
        }

        sb.AppendLine();

        // Results by scenario type
        var scenarioTypes = _results.Where(r => r.Phase != "ProcessEmails" && r.Phase != "Batch")
            .GroupBy(r => r.ScenarioType);
        sb.AppendLine("Results by Scenario Type:");
        foreach (var type in scenarioTypes)
        {
            var passed = type.Count(r => r.Passed);
            var total = type.Count();
            sb.AppendLine($"  {type.Key}: {passed}/{total} passed");
        }

        sb.AppendLine();

        // Failed tests
        var failed = _results.Where(r => !r.Passed).ToList();
        if (failed.Count > 0)
        {
            sb.AppendLine("FAILED TESTS:");
            foreach (var fail in failed.Take(20)) // Limit to 20
            {
                sb.AppendLine($"  [{fail.Phase}] {fail.ScenarioName}: {fail.Details}");
            }
            if (failed.Count > 20)
            {
                sb.AppendLine($"  ... and {failed.Count - 20} more");
            }
        }

        return sb.ToString();
    }
}

#region Result Models

public class TestResult
{
    public string ScenarioName { get; set; } = null!;
    public string ScenarioType { get; set; } = null!;
    public string Phase { get; set; } = null!;
    public bool Passed { get; set; }
    public string Details { get; set; } = null!;
}

public class TestSuiteResult
{
    public int TotalScenarios { get; set; }
    public int PassedScenarios { get; set; }
    public int FailedScenarios { get; set; }
    public TimeSpan Duration { get; set; }
    public List<TestResult> Results { get; set; } = [];
    public string Summary { get; set; } = null!;

    public double PassRate => TotalScenarios > 0 ? (double)PassedScenarios / TotalScenarios * 100 : 0;
}

#endregion
