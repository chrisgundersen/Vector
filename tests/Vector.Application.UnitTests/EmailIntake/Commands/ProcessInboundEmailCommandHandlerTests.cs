using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;
using Vector.Application.EmailIntake.Commands;
using Vector.Domain.EmailIntake;
using Vector.Domain.EmailIntake.Aggregates;
using Vector.Domain.EmailIntake.ValueObjects;

namespace Vector.Application.UnitTests.EmailIntake.Commands;

public class ProcessInboundEmailCommandHandlerTests
{
    private readonly Mock<IInboundEmailRepository> _emailRepositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<ProcessInboundEmailCommandHandler>> _loggerMock;
    private readonly ProcessInboundEmailCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public ProcessInboundEmailCommandHandlerTests()
    {
        _emailRepositoryMock = new Mock<IInboundEmailRepository>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<ProcessInboundEmailCommandHandler>>();

        _handler = new ProcessInboundEmailCommandHandler(
            _emailRepositoryMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEmail_ReturnsSuccessWithEmailId()
    {
        // Arrange
        var command = CreateValidCommand();

        _cacheServiceMock.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailRepositoryMock.Setup(x => x.ExistsByContentHashAsync(
                It.IsAny<Guid>(), It.IsAny<ContentHash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailRepositoryMock.Setup(x => x.AddAsync(It.IsAny<InboundEmail>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _emailRepositoryMock.Verify(x => x.AddAsync(It.IsAny<InboundEmail>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(), true, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmailInCache_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();

        _cacheServiceMock.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InboundEmail.Duplicate");

        _emailRepositoryMock.Verify(x => x.AddAsync(It.IsAny<InboundEmail>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmailInDatabase_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();

        _cacheServiceMock.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailRepositoryMock.Setup(x => x.ExistsByContentHashAsync(
                It.IsAny<Guid>(), It.IsAny<ContentHash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InboundEmail.Duplicate");

        _emailRepositoryMock.Verify(x => x.AddAsync(It.IsAny<InboundEmail>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(), true, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidFromAddress_ReturnsFailure()
    {
        // Arrange
        var command = new ProcessInboundEmailCommand(
            _tenantId,
            "mailbox@test.com",
            "msg-123",
            "invalid-email",
            "Test Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);

        _cacheServiceMock.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailRepositoryMock.Setup(x => x.ExistsByContentHashAsync(
                It.IsAny<Guid>(), It.IsAny<ContentHash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmailAddress.InvalidFormat");
    }

    [Fact]
    public async Task Handle_WithEmptyTenantId_ReturnsFailure()
    {
        // Arrange
        var command = new ProcessInboundEmailCommand(
            Guid.Empty,
            "mailbox@test.com",
            "msg-123",
            "sender@example.com",
            "Test Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);

        _cacheServiceMock.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailRepositoryMock.Setup(x => x.ExistsByContentHashAsync(
                It.IsAny<Guid>(), It.IsAny<ContentHash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InboundEmail.InvalidTenant");
    }

    [Fact]
    public async Task Handle_WithEmptyExternalMessageId_ReturnsFailure()
    {
        // Arrange
        var command = new ProcessInboundEmailCommand(
            _tenantId,
            "mailbox@test.com",
            "",
            "sender@example.com",
            "Test Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);

        _cacheServiceMock.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailRepositoryMock.Setup(x => x.ExistsByContentHashAsync(
                It.IsAny<Guid>(), It.IsAny<ContentHash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InboundEmail.ExternalMessageIdRequired");
    }

    private ProcessInboundEmailCommand CreateValidCommand()
    {
        return new ProcessInboundEmailCommand(
            _tenantId,
            "mailbox@test.com",
            "msg-123",
            "sender@example.com",
            "Test Subject",
            "Body preview",
            "Body content",
            DateTime.UtcNow);
    }
}
