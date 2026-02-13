using Vector.Infrastructure.Services;

namespace Vector.Infrastructure.IntegrationTests.Services;

public class DateTimeProviderTests
{
    private readonly DateTimeProvider _dateTimeProvider;

    public DateTimeProviderTests()
    {
        _dateTimeProvider = new DateTimeProvider();
    }

    [Fact]
    public void UtcNow_ReturnsCurrentUtcTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var result = _dateTimeProvider.UtcNow;

        // Assert
        var after = DateTime.UtcNow;
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Today_ReturnsCurrentUtcDate()
    {
        // Arrange
        var expected = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = _dateTimeProvider.Today;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void UtcNow_MultipleCallsIncrease()
    {
        // Act
        var first = _dateTimeProvider.UtcNow;
        Thread.Sleep(1);
        var second = _dateTimeProvider.UtcNow;

        // Assert
        second.Should().BeOnOrAfter(first);
    }
}
