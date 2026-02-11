using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using UploadsApi.Application.Interfaces;
using UploadsApi.Infrastructure.Options;

namespace UploadsApi.Infrastructure.Storage;

public class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureBlobOptions _options;

    public AzureBlobStorageService(BlobServiceClient blobServiceClient, IOptions<AzureBlobOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
    }

    public Task<string> InitiateMultipartUploadAsync(
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Azure Blob Storage doesn't require explicit multipart upload initiation.
        // Blocks are staged independently and committed at the end.
        // We return the objectKey as a pseudo-uploadId since we need a consistent identifier.
        return Task.FromResult(objectKey);
    }

    public Task<IReadOnlyList<PresignedUrlInfo>> GeneratePresignedUrlsAsync(
        string objectKey,
        string uploadId,
        int totalParts,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlockBlobClient(objectKey);

        var urls = new List<PresignedUrlInfo>();
        var expiresOn = DateTimeOffset.UtcNow.AddMinutes(_options.SasExpirationMinutes);

        for (var partNumber = 1; partNumber <= totalParts; partNumber++)
        {
            var blockId = GenerateBlockId(partNumber);

            // Create SAS token for the blob
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.ContainerName,
                BlobName = objectKey,
                Resource = "b", // blob
                ExpiresOn = expiresOn
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Write);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            // Append block query parameters to the SAS URL
            var urlWithBlockId = $"{sasUri}&comp=block&blockid={Uri.EscapeDataString(blockId)}";

            // Rewrite URL for public/browser access if PublicBlobEndpoint is configured
            if (!string.IsNullOrEmpty(_options.PublicBlobEndpoint))
            {
                var internalEndpoint = _blobServiceClient.Uri.ToString().TrimEnd('/');
                var publicEndpoint = _options.PublicBlobEndpoint.TrimEnd('/');
                urlWithBlockId = urlWithBlockId.Replace(internalEndpoint, publicEndpoint);
            }

            urls.Add(new PresignedUrlInfo(partNumber, urlWithBlockId));
        }

        return Task.FromResult<IReadOnlyList<PresignedUrlInfo>>(urls);
    }

    public async Task CompleteMultipartUploadAsync(
        string objectKey,
        string uploadId,
        IEnumerable<PartInfo> parts,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlockBlobClient(objectKey);

        // Generate block IDs in order from part numbers
        var blockIds = parts
            .OrderBy(p => p.PartNumber)
            .Select(p => GenerateBlockId(p.PartNumber))
            .ToList();

        // Commit all staged blocks to create the final blob
        await blobClient.CommitBlockListAsync(blockIds, cancellationToken: cancellationToken);
    }

    public async Task AbortMultipartUploadAsync(
        string objectKey,
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        // In Azure Blob Storage, uncommitted blocks automatically expire after 7 days.
        // Optionally, we can delete the blob if it exists (in case of partial commits).
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlobClient(objectKey);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task DeleteObjectAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlobClient(objectKey);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Generates a Base64-encoded block ID from the part number.
    /// Azure requires block IDs to be consistent in length and Base64-encoded.
    /// </summary>
    private static string GenerateBlockId(int partNumber)
    {
        // Pad to 6 digits to ensure consistent length (supports up to 999,999 parts)
        var paddedPartNumber = partNumber.ToString("D6");
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(paddedPartNumber));
    }
}
