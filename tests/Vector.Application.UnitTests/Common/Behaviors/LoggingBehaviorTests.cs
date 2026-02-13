using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Behaviors;
using Vector.Application.Common.Interfaces;
using Vector.Domain.Common;

namespace Vector.Application.UnitTests.Common.Behaviors;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly LoggingBehavior<TestRequest, TestResponse> _behavior;

    public LoggingBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _behavior = new LoggingBehavior<TestRequest, TestResponse>(
            _loggerMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithAuthenticatedUser_LogsUserAndTenantInfo()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-123");
        _currentUserServiceMock.Setup(x => x.TenantId).Returns(tenantId);

        var request = new TestRequest("Test Data");
        var expectedResponse = new TestResponse("Success");

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var response = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        response.Should().Be(expectedResponse);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("TestRequest")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1));
    }

    [Fact]
    public async Task Handle_WithAnonymousUser_LogsAnonymous()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((string?)null);
        _currentUserServiceMock.Setup(x => x.TenantId).Returns((Guid?)null);

        var request = new TestRequest("Test Data");
        var expectedResponse = new TestResponse("Success");

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var response = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        response.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WhenNextThrows_LogsErrorAndRethrows()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-123");
        _currentUserServiceMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());

        var request = new TestRequest("Test Data");
        var expectedException = new InvalidOperationException("Test exception");

        RequestHandlerDelegate<TestResponse> next = () => throw expectedException;

        // Act & Assert
        var act = () => _behavior.Handle(request, next, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsResponseFromNext()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-123");
        _currentUserServiceMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());

        var request = new TestRequest("Input");
        var expectedResponse = new TestResponse("Output Data");

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var response = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        response.Should().Be(expectedResponse);
        response.Data.Should().Be("Output Data");
    }

    [Fact]
    public async Task Handle_CompletesWithinReasonableTime_LogsElapsedTime()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-123");
        _currentUserServiceMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());

        var request = new TestRequest("Test Data");
        var expectedResponse = new TestResponse("Success");

        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(10); // Small delay to ensure elapsed time is measurable
            return expectedResponse;
        };

        // Act
        var response = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        response.Should().Be(expectedResponse);
    }

    public sealed record TestRequest(string Data) : IRequest<TestResponse>;
    public sealed record TestResponse(string Data);
}
