using MediatR;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Behaviors;
using Vector.Domain.Common;

namespace Vector.Application.UnitTests.Common.Behaviors;

public class TransactionBehaviorTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<TransactionBehavior<TestTransactionalCommand, TestCommandResult>>> _loggerMock;
    private readonly TransactionBehavior<TestTransactionalCommand, TestCommandResult> _behavior;

    public TransactionBehaviorTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<TransactionBehavior<TestTransactionalCommand, TestCommandResult>>>();
        _behavior = new TransactionBehavior<TestTransactionalCommand, TestCommandResult>(
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSuccessful_SavesChanges()
    {
        // Arrange
        var command = new TestTransactionalCommand("Test Data");
        var expectedResult = new TestCommandResult(Guid.NewGuid(), true);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        RequestHandlerDelegate<TestCommandResult> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_DoesNotSaveChanges()
    {
        // Arrange
        var command = new TestTransactionalCommand("Test Data");
        var expectedException = new InvalidOperationException("Handler failed");

        RequestHandlerDelegate<TestCommandResult> next = () => throw expectedException;

        // Act & Assert
        var act = () => _behavior.Handle(command, next, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Handler failed");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFails_ThrowsException()
    {
        // Arrange
        var command = new TestTransactionalCommand("Test Data");
        var expectedResult = new TestCommandResult(Guid.NewGuid(), true);
        var saveException = new InvalidOperationException("Database error");

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(saveException);

        RequestHandlerDelegate<TestCommandResult> next = () => Task.FromResult(expectedResult);

        // Act & Assert
        var act = () => _behavior.Handle(command, next, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    [Fact]
    public async Task Handle_LogsTransactionStart()
    {
        // Arrange
        var command = new TestTransactionalCommand("Test Data");
        var expectedResult = new TestCommandResult(Guid.NewGuid(), true);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        RequestHandlerDelegate<TestCommandResult> next = () => Task.FromResult(expectedResult);

        // Act
        await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Beginning transaction")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LogsTransactionCommit()
    {
        // Arrange
        var command = new TestTransactionalCommand("Test Data");
        var expectedResult = new TestCommandResult(Guid.NewGuid(), true);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        RequestHandlerDelegate<TestCommandResult> next = () => Task.FromResult(expectedResult);

        // Act
        await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Committed transaction")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFails_LogsError()
    {
        // Arrange
        var command = new TestTransactionalCommand("Test Data");
        var expectedException = new InvalidOperationException("Handler failed");

        RequestHandlerDelegate<TestCommandResult> next = () => throw expectedException;

        // Act
        try
        {
            await _behavior.Handle(command, next, CancellationToken.None);
        }
        catch
        {
            // Expected
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Transaction failed")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        // Arrange
        var command = new TestTransactionalCommand("Test Data");
        var expectedResult = new TestCommandResult(Guid.NewGuid(), true);
        var cancellationToken = new CancellationToken();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        RequestHandlerDelegate<TestCommandResult> next = () => Task.FromResult(expectedResult);

        // Act
        await _behavior.Handle(command, next, cancellationToken);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
    }

    public sealed record TestTransactionalCommand(string Data) : IRequest<TestCommandResult>, ITransactionalCommand;
    public sealed record TestCommandResult(Guid Id, bool Success);
}
