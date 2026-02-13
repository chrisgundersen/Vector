using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Vector.Application.Common.Behaviors;

namespace Vector.Application.UnitTests.Common.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_CallsNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, TestResult>(validators);

        var command = new TestCommand("Test Name", "test@example.com");
        var expectedResult = new TestResult(Guid.NewGuid());

        RequestHandlerDelegate<TestResult> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WithValidInput_CallsNext()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<TestCommand>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { validatorMock.Object };
        var behavior = new ValidationBehavior<TestCommand, TestResult>(validators);

        var command = new TestCommand("Test Name", "test@example.com");
        var expectedResult = new TestResult(Guid.NewGuid());

        RequestHandlerDelegate<TestResult> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WithInvalidInput_ThrowsValidationException()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
            new("Email", "Email must be valid")
        };

        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<TestCommand>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var validators = new[] { validatorMock.Object };
        var behavior = new ValidationBehavior<TestCommand, TestResult>(validators);

        var command = new TestCommand("", "invalid-email");
        var expectedResult = new TestResult(Guid.NewGuid());

        RequestHandlerDelegate<TestResult> next = () => Task.FromResult(expectedResult);

        // Act & Assert
        var act = () => behavior.Handle(command, next, CancellationToken.None);
        var exception = await act.Should().ThrowAsync<ValidationException>();

        exception.Which.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_RunsAll()
    {
        // Arrange
        var validator1Mock = new Mock<IValidator<TestCommand>>();
        validator1Mock.Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<TestCommand>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validator2Mock = new Mock<IValidator<TestCommand>>();
        validator2Mock.Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<TestCommand>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { validator1Mock.Object, validator2Mock.Object };
        var behavior = new ValidationBehavior<TestCommand, TestResult>(validators);

        var command = new TestCommand("Test Name", "test@example.com");
        var expectedResult = new TestResult(Guid.NewGuid());

        RequestHandlerDelegate<TestResult> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        validator1Mock.Verify(v => v.ValidateAsync(
            It.IsAny<ValidationContext<TestCommand>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        validator2Mock.Verify(v => v.ValidateAsync(
            It.IsAny<ValidationContext<TestCommand>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleValidatorsAndFailures_AggregatesAllFailures()
    {
        // Arrange
        var failures1 = new List<ValidationFailure>
        {
            new("Name", "Name is required")
        };

        var failures2 = new List<ValidationFailure>
        {
            new("Email", "Email must be valid")
        };

        var validator1Mock = new Mock<IValidator<TestCommand>>();
        validator1Mock.Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<TestCommand>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures1));

        var validator2Mock = new Mock<IValidator<TestCommand>>();
        validator2Mock.Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<TestCommand>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures2));

        var validators = new[] { validator1Mock.Object, validator2Mock.Object };
        var behavior = new ValidationBehavior<TestCommand, TestResult>(validators);

        var command = new TestCommand("", "invalid-email");

        RequestHandlerDelegate<TestResult> next = () => Task.FromResult(new TestResult(Guid.NewGuid()));

        // Act & Assert
        var act = () => behavior.Handle(command, next, CancellationToken.None);
        var exception = await act.Should().ThrowAsync<ValidationException>();

        exception.Which.Errors.Should().HaveCount(2);
        exception.Which.Errors.Should().Contain(f => f.PropertyName == "Name");
        exception.Which.Errors.Should().Contain(f => f.PropertyName == "Email");
    }

    [Fact]
    public async Task Handle_WhenValidationFails_DoesNotCallNext()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required")
        };

        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<TestCommand>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var validators = new[] { validatorMock.Object };
        var behavior = new ValidationBehavior<TestCommand, TestResult>(validators);

        var command = new TestCommand("", "test@example.com");
        var nextCalled = false;

        RequestHandlerDelegate<TestResult> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResult(Guid.NewGuid()));
        };

        // Act & Assert
        var act = () => behavior.Handle(command, next, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();

        nextCalled.Should().BeFalse();
    }

    public sealed record TestCommand(string Name, string Email) : IRequest<TestResult>;
    public sealed record TestResult(Guid Id);
}
