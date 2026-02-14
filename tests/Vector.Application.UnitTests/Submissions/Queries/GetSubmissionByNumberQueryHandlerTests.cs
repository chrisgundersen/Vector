using FluentAssertions;
using Moq;
using Vector.Application.Common.Interfaces;
using Vector.Application.Submissions.Queries;
using Vector.Domain.Submission;
using Vector.Domain.Submission.Aggregates;

namespace Vector.Application.UnitTests.Submissions.Queries;

public class GetSubmissionByNumberQueryHandlerTests
{
    private readonly Mock<ISubmissionRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly GetSubmissionByNumberQueryHandler _handler;

    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public GetSubmissionByNumberQueryHandlerTests()
    {
        _repositoryMock = new Mock<ISubmissionRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.TenantId).Returns(TenantId);
        _handler = new GetSubmissionByNumberQueryHandler(_repositoryMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingSubmission_ReturnsSubmissionDto()
    {
        // Arrange
        var submissionNumber = "SUB-2024-000001";
        var submission = Submission.Create(TenantId, submissionNumber, "Test Company").Value;

        _repositoryMock
            .Setup(x => x.GetBySubmissionNumberAsync(TenantId, submissionNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var query = new GetSubmissionByNumberQuery(submissionNumber);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SubmissionNumber.Should().Be(submissionNumber);
    }

    [Fact]
    public async Task Handle_WithNonExistingSubmission_ReturnsNull()
    {
        // Arrange
        var submissionNumber = "SUB-NONEXISTENT";

        _repositoryMock
            .Setup(x => x.GetBySubmissionNumberAsync(TenantId, submissionNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        var query = new GetSubmissionByNumberQuery(submissionNumber);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNoTenantId_ThrowsInvalidOperationException()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.TenantId).Returns((Guid?)null);
        var handler = new GetSubmissionByNumberQueryHandler(_repositoryMock.Object, _currentUserServiceMock.Object);

        var query = new GetSubmissionByNumberQuery("SUB-2024-000001");

        // Act
        var act = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tenant ID is required");
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var submissionNumber = "SUB-2024-TEST";

        _repositoryMock
            .Setup(x => x.GetBySubmissionNumberAsync(TenantId, submissionNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        var query = new GetSubmissionByNumberQuery(submissionNumber);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            x => x.GetBySubmissionNumberAsync(TenantId, submissionNumber, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
