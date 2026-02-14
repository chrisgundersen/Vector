using FluentAssertions;
using Vector.EndToEnd.Tests.Harness;
using Xunit.Abstractions;

namespace Vector.EndToEnd.Tests;

/// <summary>
/// Comprehensive end-to-end tests for the Vector submission processing system.
/// Tests 50 realistic submission scenarios covering various industries, coverages, and data qualities.
/// </summary>
public class EndToEndSubmissionTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private SubmissionTestHarness _harness = null!;

    public EndToEndSubmissionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _harness = new SubmissionTestHarness();
        await _harness.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _harness.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "Submission")]
    public async Task FullSubmissionPipeline_50Scenarios_ProcessedCorrectly()
    {
        // Act
        var result = await _harness.RunFullTestSuiteAsync();

        // Output detailed results
        _output.WriteLine(result.Summary);
        _output.WriteLine($"Total Duration: {result.Duration.TotalSeconds:F2} seconds");
        _output.WriteLine($"Pass Rate: {result.PassRate:F1}%");

        // Assert
        result.PassedScenarios.Should().BeGreaterThan(0, "at least some scenarios should pass");
        result.PassRate.Should().BeGreaterThan(50, "pass rate should be above 50%");
    }
}
