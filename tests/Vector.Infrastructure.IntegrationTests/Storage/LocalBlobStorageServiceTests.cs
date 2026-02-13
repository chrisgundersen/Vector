using System.Text;
using Microsoft.Extensions.Logging;
using Vector.Infrastructure.Storage;

namespace Vector.Infrastructure.IntegrationTests.Storage;

public class LocalBlobStorageServiceTests : IDisposable
{
    private readonly LocalBlobStorageService _storageService;
    private readonly string _testContainer = $"test-container-{Guid.NewGuid():N}";

    public LocalBlobStorageServiceTests()
    {
        var loggerMock = new Mock<ILogger<LocalBlobStorageService>>();
        _storageService = new LocalBlobStorageService(loggerMock.Object);
    }

    public void Dispose()
    {
        // Clean up test files
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vector", "BlobStorage", _testContainer);

        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAsync_CreatesFileAndReturnsUrl()
    {
        // Arrange
        var blobName = $"test-{Guid.NewGuid()}.txt";
        var content = Encoding.UTF8.GetBytes("Test content");
        using var stream = new MemoryStream(content);

        // Act
        var url = await _storageService.UploadAsync(
            _testContainer,
            blobName,
            stream,
            "text/plain");

        // Assert
        url.Should().NotBeNullOrEmpty();
        url.Should().StartWith("file:///");
        url.Should().Contain(blobName);
    }

    [Fact]
    public async Task ExistsAsync_AfterUpload_ReturnsTrue()
    {
        // Arrange
        var blobName = $"test-{Guid.NewGuid()}.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));
        await _storageService.UploadAsync(_testContainer, blobName, stream, "text/plain");

        // Act
        var exists = await _storageService.ExistsAsync(_testContainer, blobName);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentBlob_ReturnsFalse()
    {
        // Act
        var exists = await _storageService.ExistsAsync(_testContainer, "non-existent.txt");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DownloadAsync_ReturnsUploadedContent()
    {
        // Arrange
        var blobName = $"test-{Guid.NewGuid()}.txt";
        var originalContent = "Test content for download";
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
        await _storageService.UploadAsync(_testContainer, blobName, uploadStream, "text/plain");

        // Act
        using var downloadStream = await _storageService.DownloadAsync(_testContainer, blobName);
        using var reader = new StreamReader(downloadStream);
        var downloadedContent = await reader.ReadToEndAsync();

        // Assert
        downloadedContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task DownloadAsync_WithNonExistentBlob_ThrowsFileNotFoundException()
    {
        // Act & Assert
        var act = () => _storageService.DownloadAsync(_testContainer, "non-existent.txt");
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesFile()
    {
        // Arrange
        var blobName = $"test-{Guid.NewGuid()}.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));
        await _storageService.UploadAsync(_testContainer, blobName, stream, "text/plain");

        // Act
        await _storageService.DeleteAsync(_testContainer, blobName);

        // Assert
        var exists = await _storageService.ExistsAsync(_testContainer, blobName);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentBlob_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        await _storageService.DeleteAsync(_testContainer, "non-existent.txt");
    }

    [Fact]
    public async Task GetSasUrlAsync_ReturnsFileUrl()
    {
        // Arrange
        var blobName = $"test-{Guid.NewGuid()}.txt";

        // Act
        var sasUrl = await _storageService.GetSasUrlAsync(
            _testContainer,
            blobName,
            TimeSpan.FromHours(1));

        // Assert
        sasUrl.Should().StartWith("file:///");
        sasUrl.Should().Contain(blobName);
    }

    [Fact]
    public async Task UploadAsync_WithBinaryContent_PreservesData()
    {
        // Arrange
        var blobName = $"test-{Guid.NewGuid()}.bin";
        var binaryContent = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD };
        using var uploadStream = new MemoryStream(binaryContent);

        // Act
        await _storageService.UploadAsync(_testContainer, blobName, uploadStream, "application/octet-stream");
        using var downloadStream = await _storageService.DownloadAsync(_testContainer, blobName);
        using var memoryStream = new MemoryStream();
        await downloadStream.CopyToAsync(memoryStream);
        var downloadedContent = memoryStream.ToArray();

        // Assert
        downloadedContent.Should().Equal(binaryContent);
    }

    [Fact]
    public async Task UploadAsync_WithNestedPath_CreatesDirectories()
    {
        // Arrange
        var blobName = $"folder1/folder2/test-{Guid.NewGuid()}.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));

        // Act
        var url = await _storageService.UploadAsync(_testContainer, blobName, stream, "text/plain");

        // Assert
        url.Should().Contain("folder1");
        url.Should().Contain("folder2");

        var exists = await _storageService.ExistsAsync(_testContainer, blobName);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_Overwrite_UpdatesContent()
    {
        // Arrange
        var blobName = $"test-{Guid.NewGuid()}.txt";
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Original content"));
        await _storageService.UploadAsync(_testContainer, blobName, stream1, "text/plain");

        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("New content"));

        // Act
        await _storageService.UploadAsync(_testContainer, blobName, stream2, "text/plain");

        // Assert
        using var downloadStream = await _storageService.DownloadAsync(_testContainer, blobName);
        using var reader = new StreamReader(downloadStream);
        var content = await reader.ReadToEndAsync();
        content.Should().Be("New content");
    }
}
