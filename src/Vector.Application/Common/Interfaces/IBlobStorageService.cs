namespace Vector.Application.Common.Interfaces;

/// <summary>
/// Interface for blob storage operations.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file to blob storage.
    /// </summary>
    Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from blob storage.
    /// </summary>
    Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a SAS URL for temporary access to a blob.
    /// </summary>
    Task<string> GetSasUrlAsync(
        string containerName,
        string blobName,
        TimeSpan validFor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob from storage.
    /// </summary>
    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists.
    /// </summary>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
}
