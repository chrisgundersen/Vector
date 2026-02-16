using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;
using Vector.Infrastructure.Services;

namespace Vector.Infrastructure.IntegrationTests.Services;

public class ClearanceCheckServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<ISubmissionRepository> _repositoryMock;
    private readonly ClearanceCheckService _service;

    public ClearanceCheckServiceTests()
    {
        _repositoryMock = new Mock<ISubmissionRepository>();
        _service = new ClearanceCheckService(_repositoryMock.Object);
    }

    private Submission CreateSubmissionWithFein(string submissionNumber, string insuredName, string fein)
    {
        var sub = Submission.Create(_tenantId, submissionNumber, insuredName).Value;
        sub.MarkAsReceived();
        sub.Insured.UpdateFein(fein);
        return sub;
    }

    private Submission CreateSubmissionWithAddress(string submissionNumber, string insuredName, string street, string city, string state, string zip)
    {
        var sub = Submission.Create(_tenantId, submissionNumber, insuredName).Value;
        sub.MarkAsReceived();
        var addr = Address.Create(street, null, city, state, zip).Value;
        sub.Insured.UpdateMailingAddress(addr);
        return sub;
    }

    [Fact]
    public async Task CheckAsync_WithExactFeinMatch_ReturnsMatch()
    {
        var submission = CreateSubmissionWithFein("SUB-2024-000001", "Test Co", "12-3456789");
        var existing = CreateSubmissionWithFein("SUB-2024-000002", "Other Co", "12-3456789");

        _repositoryMock.Setup(x => x.FindPotentialDuplicatesAsync(
                _tenantId, submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Submission> { existing });

        var matches = await _service.CheckAsync(submission);

        matches.Should().ContainSingle();
        matches[0].MatchType.Should().Be(ClearanceMatchType.FeinMatch);
        matches[0].ConfidenceScore.Should().Be(1.0);
    }

    [Fact]
    public async Task CheckAsync_WithFuzzyNameMatch_ReturnsMatch()
    {
        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Acme Manufacturing Inc").Value;
        submission.MarkAsReceived();

        var existing = Submission.Create(_tenantId, "SUB-2024-000002", "Acme Manufacturing Corp").Value;
        existing.MarkAsReceived();

        _repositoryMock.Setup(x => x.FindPotentialDuplicatesAsync(
                _tenantId, submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Submission> { existing });

        var matches = await _service.CheckAsync(submission);

        matches.Should().Contain(m => m.MatchType == ClearanceMatchType.NameMatch);
    }

    [Fact]
    public async Task CheckAsync_WithNormalizedAddressMatch_ReturnsMatch()
    {
        var submission = CreateSubmissionWithAddress("SUB-2024-000001", "Test Co", "123 Main Street", "New York", "NY", "10001");
        var existing = CreateSubmissionWithAddress("SUB-2024-000002", "Other Co", "123 Main St", "New York", "NY", "10001");

        _repositoryMock.Setup(x => x.FindPotentialDuplicatesAsync(
                _tenantId, submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Submission> { existing });

        var matches = await _service.CheckAsync(submission);

        matches.Should().Contain(m => m.MatchType == ClearanceMatchType.AddressMatch);
    }

    [Fact]
    public async Task CheckAsync_WithNoMatches_ReturnsEmpty()
    {
        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Unique Company XYZ").Value;
        submission.MarkAsReceived();

        var existing = Submission.Create(_tenantId, "SUB-2024-000002", "Completely Different Company").Value;
        existing.MarkAsReceived();

        _repositoryMock.Setup(x => x.FindPotentialDuplicatesAsync(
                _tenantId, submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Submission> { existing });

        var matches = await _service.CheckAsync(submission);

        matches.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAsync_WithNoCandidates_ReturnsEmpty()
    {
        var submission = Submission.Create(_tenantId, "SUB-2024-000001", "Test Co").Value;
        submission.MarkAsReceived();

        _repositoryMock.Setup(x => x.FindPotentialDuplicatesAsync(
                _tenantId, submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Submission>());

        var matches = await _service.CheckAsync(submission);

        matches.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeName_RemovesEntitySuffixes()
    {
        ClearanceCheckService.NormalizeName("Acme Manufacturing Inc").Should().Be("ACME MANUFACTURING");
        ClearanceCheckService.NormalizeName("Test Company LLC").Should().Be("TEST COMPANY");
        ClearanceCheckService.NormalizeName("Delta Corp.").Should().Be("DELTA");
    }

    [Fact]
    public void NormalizeFein_RemovesNonDigits()
    {
        ClearanceCheckService.NormalizeFein("12-3456789").Should().Be("123456789");
        ClearanceCheckService.NormalizeFein("123456789").Should().Be("123456789");
    }

    [Fact]
    public void NormalizeAddress_AbbreviatesCommonTerms()
    {
        var result = ClearanceCheckService.NormalizeAddress("123 Main Street", "New York", "NY", "10001");
        result.Should().Contain("MAIN ST");
    }

    [Fact]
    public void CalculateSimilarity_IdenticalStrings_ReturnsOne()
    {
        ClearanceCheckService.CalculateSimilarity("ABC", "ABC").Should().Be(1.0);
    }

    [Fact]
    public void CalculateSimilarity_CompletelyDifferent_ReturnsLowScore()
    {
        var score = ClearanceCheckService.CalculateSimilarity("ABCDEF", "XYZWVQ");
        score.Should().BeLessThan(0.5);
    }

    [Fact]
    public void CalculateSimilarity_SimilarStrings_ReturnsHighScore()
    {
        var score = ClearanceCheckService.CalculateSimilarity("ACME MANUFACTURING", "ACME MNUFACTURING");
        score.Should().BeGreaterThan(0.75);
    }

    [Fact]
    public void LevenshteinDistance_IdenticalStrings_ReturnsZero()
    {
        ClearanceCheckService.LevenshteinDistance("hello", "hello").Should().Be(0);
    }

    [Fact]
    public void LevenshteinDistance_SingleEdit_ReturnsOne()
    {
        ClearanceCheckService.LevenshteinDistance("hello", "hallo").Should().Be(1);
    }
}
