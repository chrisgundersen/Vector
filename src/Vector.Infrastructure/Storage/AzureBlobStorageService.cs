using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage implementation.
/// </summary>
public class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    ILogger<AzureBlobStorageService> logger) : IBlobStorageService
{
    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            content,
            new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);

        logger.LogDebug("Uploaded blob {Container}/{Blob}", containerName, blobName);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadAsync(cancellationToken);
        return response.Value.Content;
    }

    public Task<string> GetSasUrlAsync(
        string containerName,
        string blobName,
        TimeSpan validFor,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var sasUri = blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(validFor));

        return Task.FromResult(sasUri.ToString());
    }

    public async Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

        logger.LogDebug("Deleted blob {Container}/{Blob}", containerName, blobName);
    }

    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        return await blobClient.ExistsAsync(cancellationToken);
    }
}
