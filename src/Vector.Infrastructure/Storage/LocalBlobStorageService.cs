using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Storage;

/// <summary>
/// Local file system implementation of blob storage for development/testing.
/// </summary>
public class LocalBlobStorageService(
    ILogger<LocalBlobStorageService> logger) : IBlobStorageService
{
    private readonly string _basePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Vector", "BlobStorage");

    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(containerName, blobName);
        var directory = Path.GetDirectoryName(fullPath)!;

        Directory.CreateDirectory(directory);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        logger.LogDebug("Uploaded blob to {Path}", fullPath);

        return $"file:///{fullPath.Replace('\\', '/')}";
    }

    public async Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(containerName, blobName);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Blob not found: {containerName}/{blobName}");
        }

        var memoryStream = new MemoryStream();
        await using var fileStream = File.OpenRead(fullPath);
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public Task<string> GetSasUrlAsync(
        string containerName,
        string blobName,
        TimeSpan validFor,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(containerName, blobName);
        return Task.FromResult($"file:///{fullPath.Replace('\\', '/')}");
    }

    public Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(containerName, blobName);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogDebug("Deleted blob at {Path}", fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(containerName, blobName);
        return Task.FromResult(File.Exists(fullPath));
    }

    private string GetFullPath(string containerName, string blobName)
    {
        return Path.Combine(_basePath, containerName, blobName);
    }
}
