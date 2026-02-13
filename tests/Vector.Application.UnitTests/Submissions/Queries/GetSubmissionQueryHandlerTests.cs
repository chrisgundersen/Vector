using Vector.Application.Submissions.DTOs;
using Vector.Application.Submissions.Queries;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.UnitTests.Submissions.Queries;

public class GetSubmissionQueryHandlerTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepositoryMock;
    private readonly GetSubmissionQueryHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public GetSubmissionQueryHandlerTests()
    {
        _submissionRepositoryMock = new Mock<ISubmissionRepository>();
        _handler = new GetSubmissionQueryHandler(_submissionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingSubmission_ReturnsDto()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var query = new GetSubmissionQuery(submissionId);

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(submission.Id);
        result.SubmissionNumber.Should().Be("SUB-2024-000001");
        result.Insured.Name.Should().Be("ABC Manufacturing Corp");
        result.Status.Should().Be("Received");
    }

    [Fact]
    public async Task Handle_WithNonExistentSubmission_ReturnsNull()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var query = new GetSubmissionQuery(submissionId);

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithSubmissionCoverages_ReturnsCoveragesInDto()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var query = new GetSubmissionQuery(submissionId);

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AddCoverage(Domain.Submission.Enums.CoverageType.GeneralLiability);
        submission.AddCoverage(Domain.Submission.Enums.CoverageType.PropertyDamage);

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Coverages.Should().HaveCount(2);
        result.Coverages.Select(c => c.Type).Should().Contain("GeneralLiability");
        result.Coverages.Select(c => c.Type).Should().Contain("PropertyDamage");
    }

    [Fact]
    public async Task Handle_WithSubmissionLocations_ReturnsLocationsInDto()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var query = new GetSubmissionQuery(submissionId);

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();

        var addressResult = Domain.Submission.ValueObjects.Address.Create(
            "123 Main St", null, "Austin", "TX", "78701", "USA");
        submission.AddLocation(addressResult.Value);

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Locations.Should().HaveCount(1);
        result.Locations[0].LocationNumber.Should().Be(1);
        result.Locations[0].Address.Street1.Should().Be("123 Main St");
        result.Locations[0].Address.City.Should().Be("Austin");
    }

    [Fact]
    public async Task Handle_WithAssignedSubmission_ReturnsUnderwriterInfo()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var query = new GetSubmissionQuery(submissionId);

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.AssignToUnderwriter(Guid.NewGuid(), "John Underwriter");

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.AssignedUnderwriterName.Should().Be("John Underwriter");
        result.AssignedAt.Should().NotBeNull();
        result.Status.Should().Be("InReview");
    }

    [Fact]
    public async Task Handle_WithScoresSet_ReturnsScoresInDto()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var query = new GetSubmissionQuery(submissionId);

        var submissionResult = Submission.Create(
            _tenantId,
            "SUB-2024-000001",
            "ABC Manufacturing Corp");
        var submission = submissionResult.Value;
        submission.MarkAsReceived();
        submission.UpdateScores(85, 72, 90);

        _submissionRepositoryMock.Setup(x => x.GetByIdAsync(
                submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.AppetiteScore.Should().Be(85);
        result.WinnabilityScore.Should().Be(72);
        result.DataQualityScore.Should().Be(90);
    }
}
