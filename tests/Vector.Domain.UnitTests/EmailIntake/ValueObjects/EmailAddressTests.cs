using Vector.Domain.EmailIntake.ValueObjects;

namespace Vector.Domain.UnitTests.EmailIntake.ValueObjects;

public class EmailAddressTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@subdomain.example.com")]
    public void Create_WithValidEmail_ReturnsSuccess(string email)
    {
        // Act
        var result = EmailAddress.Create(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(email.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyEmail_ReturnsFailure(string? email)
    {
        // Act
        var result = EmailAddress.Create(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmailAddress.Empty");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    public void Create_WithInvalidFormat_ReturnsFailure(string email)
    {
        // Act
        var result = EmailAddress.Create(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmailAddress.InvalidFormat");
    }

    [Fact]
    public void Domain_ReturnsCorrectDomain()
    {
        // Arrange
        var result = EmailAddress.Create("user@example.com");

        // Act & Assert
        result.Value.Domain.Should().Be("example.com");
    }

    [Fact]
    public void LocalPart_ReturnsCorrectLocalPart()
    {
        // Arrange
        var result = EmailAddress.Create("user@example.com");

        // Act & Assert
        result.Value.LocalPart.Should().Be("user");
    }

    [Fact]
    public void Create_WithTooLongEmail_ReturnsFailure()
    {
        // Arrange
        var longLocalPart = new string('a', 250);
        var tooLongEmail = $"{longLocalPart}@example.com";

        // Act
        var result = EmailAddress.Create(tooLongEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EmailAddress.TooLong");
    }

    [Fact]
    public void Create_ConvertsToLowercase()
    {
        // Act
        var result = EmailAddress.Create("TEST@EXAMPLE.COM");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        // Act
        var result = EmailAddress.Create("  test@example.com  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void ToString_ReturnsEmailValue()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;

        // Act
        var result = email.ToString();

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void Equality_WithSameEmail_AreEqual()
    {
        // Arrange
        var email1 = EmailAddress.Create("test@example.com").Value;
        var email2 = EmailAddress.Create("TEST@EXAMPLE.COM").Value;

        // Assert
        email1.Should().Be(email2);
    }

    [Fact]
    public void Equality_WithDifferentEmail_AreNotEqual()
    {
        // Arrange
        var email1 = EmailAddress.Create("test1@example.com").Value;
        var email2 = EmailAddress.Create("test2@example.com").Value;

        // Assert
        email1.Should().NotBe(email2);
    }

    [Fact]
    public void Domain_WithSubdomain_ReturnsFullDomain()
    {
        // Arrange
        var email = EmailAddress.Create("user@mail.example.com").Value;

        // Act
        var domain = email.Domain;

        // Assert
        domain.Should().Be("mail.example.com");
    }

    [Fact]
    public void LocalPart_WithComplexLocalPart_ReturnsCorrectValue()
    {
        // Arrange
        var email = EmailAddress.Create("user.name+tag@example.com").Value;

        // Act
        var localPart = email.LocalPart;

        // Assert
        localPart.Should().Be("user.name+tag");
    }
}
