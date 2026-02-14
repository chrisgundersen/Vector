using FluentAssertions;
using Vector.EndToEnd.Tests.TestData.Generators;
using Xunit.Abstractions;

namespace Vector.EndToEnd.Tests;

/// <summary>
/// Tests for the data generation utilities used in end-to-end testing.
/// These tests validate that generators produce realistic, valid test data.
/// </summary>
public class DataGenerationTests
{
    private readonly ITestOutputHelper _output;

    public DataGenerationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [Trait("Category", "DataGeneration")]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void SubmissionGenerator_GeneratesValidScenarios(int count)
    {
        // Arrange
        var generator = new SubmissionGenerator(12345);

        // Act
        var scenarios = generator.GenerateSubmissions(count);

        // Assert
        scenarios.Should().HaveCount(count);

        foreach (var scenario in scenarios)
        {
            scenario.SubmissionNumber.Should().NotBeNullOrEmpty();
            scenario.Insured.Should().NotBeNull();
            scenario.Insured.Name.Should().NotBeNullOrEmpty();
            scenario.Insured.State.Should().HaveLength(2);
            scenario.Insured.ZipCode.Should().MatchRegex(@"^\d{5}$");
            scenario.Insured.NaicsCode.Should().NotBeNullOrEmpty();
            scenario.Insured.AnnualRevenue.Should().BeGreaterThan(0);
            scenario.Producer.Should().NotBeNull();
            scenario.Producer.Email.Should().Contain("@");
            scenario.Coverages.Should().NotBeEmpty();
            scenario.Locations.Should().NotBeEmpty();
            scenario.Email.Should().NotBeNull();
            scenario.Email.Subject.Should().NotBeNullOrEmpty();
            scenario.Email.Body.Should().NotBeNullOrEmpty();
            scenario.Attachments.Should().NotBeEmpty();
        }

        // Output summary
        _output.WriteLine($"Generated {count} scenarios:");
        var byType = scenarios.GroupBy(s => s.ScenarioType);
        foreach (var group in byType.OrderBy(g => g.Key))
        {
            _output.WriteLine($"  {group.Key}: {group.Count()}");
        }

        var byQuality = scenarios.GroupBy(s => s.DataQuality);
        _output.WriteLine("\nBy Data Quality:");
        foreach (var group in byQuality.OrderBy(g => g.Key))
        {
            _output.WriteLine($"  {group.Key}: {group.Count()}");
        }

        var avgLocations = scenarios.Average(s => s.Locations.Count);
        var maxLocations = scenarios.Max(s => s.Locations.Count);
        _output.WriteLine($"\nLocation counts: Avg={avgLocations:F1}, Max={maxLocations}");
    }

    [Theory]
    [Trait("Category", "DataGeneration")]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(500)]
    public void SovGenerator_GeneratesValidSpreadsheet(int locationCount)
    {
        // Arrange
        var submissionGenerator = new SubmissionGenerator(12345);
        var scenarios = submissionGenerator.GenerateSubmissions(1);
        var scenario = scenarios.First();

        // Add more locations for larger tests
        while (scenario.Locations.Count < locationCount)
        {
            scenario.Locations.Add(new SubmissionLocation
            {
                LocationNumber = scenario.Locations.Count + 1,
                Street = $"{scenario.Locations.Count * 100} Test Street",
                City = "Test City",
                State = "TX",
                ZipCode = "75001",
                OccupancyType = "Office",
                ConstructionType = "Fire Resistive",
                YearBuilt = 2000,
                SquareFootage = 10000,
                NumberOfStories = 2,
                BuildingValue = 1000000,
                ContentsValue = 200000,
                BusinessIncomeValue = 100000,
                HasSprinklers = true,
                HasFireAlarm = true,
                HasSecuritySystem = false,
                ProtectionClass = "3"
            });
        }

        var sovGenerator = new SovGenerator(12345);

        // Act
        var excelBytes = sovGenerator.GenerateSovWorkbook(scenario.Locations, scenario.Insured);

        // Assert
        excelBytes.Should().NotBeEmpty();
        excelBytes.Length.Should().BeGreaterThan(1000, "Excel file should have reasonable size");

        // Output stats
        _output.WriteLine($"Generated SOV with {locationCount} locations");
        _output.WriteLine($"File size: {excelBytes.Length:N0} bytes ({excelBytes.Length / 1024.0:F1} KB)");

        var totalTiv = scenario.Locations.Sum(l => l.BuildingValue + l.ContentsValue + l.BusinessIncomeValue);
        _output.WriteLine($"Total TIV: ${totalTiv:N0}");
    }

    [Fact]
    [Trait("Category", "DataGeneration")]
    public void SovGenerator_LargeSov_GeneratesCorrectly()
    {
        // Arrange
        var submissionGenerator = new SubmissionGenerator(12345);
        var scenarios = submissionGenerator.GenerateSubmissions(1);
        var insured = scenarios.First().Insured;
        var sovGenerator = new SovGenerator(12345);

        // Act - Generate a large SOV with 1000 locations
        var excelBytes = sovGenerator.GenerateLargeSov(1000, insured);

        // Assert
        excelBytes.Should().NotBeEmpty();
        excelBytes.Length.Should().BeGreaterThan(100000, "Large SOV should be substantial");

        _output.WriteLine($"Generated large SOV with 1000 locations");
        _output.WriteLine($"File size: {excelBytes.Length:N0} bytes ({excelBytes.Length / 1024.0 / 1024.0:F2} MB)");
    }

    [Fact]
    [Trait("Category", "DataGeneration")]
    public void SubmissionGenerator_ScenarioDistribution_Balanced()
    {
        // Arrange
        var generator = new SubmissionGenerator(12345);

        // Act
        var scenarios = generator.GenerateSubmissions(100);

        // Assert - verify we have a good distribution of scenario types
        var byType = scenarios.GroupBy(s => s.ScenarioType).ToList();

        byType.Count.Should().BeGreaterThanOrEqualTo(5, "should have variety of scenario types");

        foreach (var group in byType)
        {
            group.Count().Should().BeGreaterThanOrEqualTo(5, $"each scenario type should have multiple instances: {group.Key}");
        }

        // Output distribution
        _output.WriteLine("Scenario Distribution:");
        foreach (var group in byType.OrderByDescending(g => g.Count()))
        {
            _output.WriteLine($"  {group.Key}: {group.Count()}");
        }
    }

    [Fact]
    [Trait("Category", "DataGeneration")]
    public void SubmissionGenerator_EmailContent_IsRealistic()
    {
        // Arrange
        var generator = new SubmissionGenerator(12345);

        // Act
        var scenarios = generator.GenerateSubmissions(10);

        // Assert
        foreach (var scenario in scenarios)
        {
            // Email subject should mention insured or coverage
            var subjectContainsRelevantInfo =
                scenario.Email.Subject.Contains(scenario.Insured.Name) ||
                scenario.Email.Subject.Contains("Submission") ||
                scenario.Coverages.Any(c => scenario.Email.Subject.Contains(c.CoverageType.ToString()));

            subjectContainsRelevantInfo.Should().BeTrue($"Email subject should be relevant: {scenario.Email.Subject}");

            // Email body should contain key information
            scenario.Email.Body.Should().Contain(scenario.Insured.Name, "Body should mention insured");
            scenario.Email.Body.Should().MatchRegex(@"\d{2}/\d{2}/\d{4}", "Body should contain dates");

            // Output sample
            if (scenario == scenarios.First())
            {
                _output.WriteLine("Sample Email:");
                _output.WriteLine($"Subject: {scenario.Email.Subject}");
                _output.WriteLine($"From: {scenario.Email.FromName} <{scenario.Email.FromAddress}>");
                _output.WriteLine("Body (first 500 chars):");
                _output.WriteLine(scenario.Email.Body.Length > 500
                    ? scenario.Email.Body.Substring(0, 500) + "..."
                    : scenario.Email.Body);
            }
        }
    }
}
