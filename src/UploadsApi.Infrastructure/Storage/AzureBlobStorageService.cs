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

    public Task<IReadOnlyList<PresignedUrlInfo>> GeneratePresignedUrlsAsync(
        string objectKey,
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

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.ContainerName,
                BlobName = objectKey,
                Resource = "b",
                ExpiresOn = expiresOn
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Write);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            var urlWithBlockId = $"{sasUri}&comp=block&blockid={Uri.EscapeDataString(blockId)}";

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

    public async Task CommitUploadAsync(
        string objectKey,
        int totalParts,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlockBlobClient(objectKey);

        var blockIds = Enumerable.Range(1, totalParts)
            .Select(GenerateBlockId)
            .ToList();

        await blobClient.CommitBlockListAsync(blockIds, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlobClient(objectKey);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public string GetBlobUrl(string objectKey)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlobClient(objectKey);

        var url = blobClient.Uri.ToString();

        if (!string.IsNullOrEmpty(_options.PublicBlobEndpoint))
        {
            var internalEndpoint = _blobServiceClient.Uri.ToString().TrimEnd('/');
            var publicEndpoint = _options.PublicBlobEndpoint.TrimEnd('/');
            url = url.Replace(internalEndpoint, publicEndpoint);
        }

        return url;
    }

    private static string GenerateBlockId(int partNumber)
    {
        var paddedPartNumber = partNumber.ToString("D6");
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(paddedPartNumber));
    }
}
