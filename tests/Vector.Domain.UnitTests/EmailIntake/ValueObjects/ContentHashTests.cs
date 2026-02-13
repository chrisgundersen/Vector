using Vector.Domain.EmailIntake.ValueObjects;

namespace Vector.Domain.UnitTests.EmailIntake.ValueObjects;

public class ContentHashTests
{
    [Fact]
    public void ComputeSha256_WithString_ComputesHash()
    {
        // Arrange
        var content = "test content";

        // Act
        var hash = ContentHash.ComputeSha256(content);

        // Assert
        hash.Should().NotBeNull();
        hash.Value.Should().NotBeNullOrEmpty();
        hash.Algorithm.Should().Be("SHA256");
    }

    [Fact]
    public void ComputeSha256_WithString_ProducesDeterministicHash()
    {
        // Arrange
        var content = "test content";

        // Act
        var hash1 = ContentHash.ComputeSha256(content);
        var hash2 = ContentHash.ComputeSha256(content);

        // Assert
        hash1.Value.Should().Be(hash2.Value);
    }

    [Fact]
    public void ComputeSha256_WithDifferentStrings_ProducesDifferentHashes()
    {
        // Arrange
        var content1 = "test content 1";
        var content2 = "test content 2";

        // Act
        var hash1 = ContentHash.ComputeSha256(content1);
        var hash2 = ContentHash.ComputeSha256(content2);

        // Assert
        hash1.Value.Should().NotBe(hash2.Value);
    }

    [Fact]
    public void ComputeSha256_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var content = "";

        // Act
        var act = () => ContentHash.ComputeSha256(content);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ComputeSha256_WithNullString_ThrowsArgumentException()
    {
        // Arrange
        string content = null!;

        // Act
        var act = () => ContentHash.ComputeSha256(content);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ComputeSha256_WithByteArray_ComputesHash()
    {
        // Arrange
        var content = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        var hash = ContentHash.ComputeSha256(content);

        // Assert
        hash.Should().NotBeNull();
        hash.Value.Should().NotBeNullOrEmpty();
        hash.Algorithm.Should().Be("SHA256");
    }

    [Fact]
    public void ComputeSha256_WithByteArray_ProducesDeterministicHash()
    {
        // Arrange
        var content = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        var hash1 = ContentHash.ComputeSha256(content);
        var hash2 = ContentHash.ComputeSha256(content);

        // Assert
        hash1.Value.Should().Be(hash2.Value);
    }

    [Fact]
    public void ComputeSha256_WithDifferentByteArrays_ProducesDifferentHashes()
    {
        // Arrange
        var content1 = new byte[] { 0x01, 0x02, 0x03 };
        var content2 = new byte[] { 0x04, 0x05, 0x06 };

        // Act
        var hash1 = ContentHash.ComputeSha256(content1);
        var hash2 = ContentHash.ComputeSha256(content2);

        // Assert
        hash1.Value.Should().NotBe(hash2.Value);
    }

    [Fact]
    public void ComputeSha256_WithNullByteArray_ThrowsArgumentNullException()
    {
        // Arrange
        byte[] content = null!;

        // Act
        var act = () => ContentHash.ComputeSha256(content);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromExisting_CreatesHashWithProvidedValues()
    {
        // Arrange
        var hashValue = "abc123def456";
        var algorithm = "SHA256";

        // Act
        var hash = ContentHash.FromExisting(hashValue, algorithm);

        // Assert
        hash.Value.Should().Be(hashValue);
        hash.Algorithm.Should().Be(algorithm);
    }

    [Fact]
    public void FromExisting_WithDefaultAlgorithm_UsesSha256()
    {
        // Arrange
        var hashValue = "abc123def456";

        // Act
        var hash = ContentHash.FromExisting(hashValue);

        // Assert
        hash.Algorithm.Should().Be("SHA256");
    }

    [Fact]
    public void FromExisting_WithCustomAlgorithm_UsesProvided()
    {
        // Arrange
        var hashValue = "abc123def456";
        var algorithm = "MD5";

        // Act
        var hash = ContentHash.FromExisting(hashValue, algorithm);

        // Assert
        hash.Algorithm.Should().Be("MD5");
    }

    [Fact]
    public void FromExisting_WithEmptyHash_ThrowsArgumentException()
    {
        // Arrange
        var hashValue = "";

        // Act
        var act = () => ContentHash.FromExisting(hashValue);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromExisting_WithNullHash_ThrowsArgumentException()
    {
        // Arrange
        string hashValue = null!;

        // Act
        var act = () => ContentHash.FromExisting(hashValue);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var hash = ContentHash.FromExisting("abc123", "SHA256");

        // Act
        var result = hash.ToString();

        // Assert
        result.Should().Be("SHA256:abc123");
    }

    [Fact]
    public void Equality_WithSameValueAndAlgorithm_AreEqual()
    {
        // Arrange
        var hash1 = ContentHash.FromExisting("abc123", "SHA256");
        var hash2 = ContentHash.FromExisting("abc123", "SHA256");

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Equality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        var hash1 = ContentHash.FromExisting("abc123", "SHA256");
        var hash2 = ContentHash.FromExisting("def456", "SHA256");

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Equality_WithDifferentAlgorithm_AreNotEqual()
    {
        // Arrange
        var hash1 = ContentHash.FromExisting("abc123", "SHA256");
        var hash2 = ContentHash.FromExisting("abc123", "MD5");

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeSha256_ProducesLowercaseHex()
    {
        // Arrange
        var content = "test content";

        // Act
        var hash = ContentHash.ComputeSha256(content);

        // Assert
        hash.Value.Should().MatchRegex("^[a-f0-9]+$");
    }

    [Fact]
    public void ComputeSha256_Produces64CharacterHash()
    {
        // Arrange
        var content = "test content";

        // Act
        var hash = ContentHash.ComputeSha256(content);

        // Assert
        hash.Value.Should().HaveLength(64);
    }
}
