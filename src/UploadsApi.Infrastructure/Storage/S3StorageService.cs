using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using UploadsApi.Application.Interfaces;
using UploadsApi.Infrastructure.Options;

namespace UploadsApi.Infrastructure.Storage;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _options;

    public S3StorageService(IAmazonS3 s3Client, IOptions<S3Options> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task<string> InitiateMultipartUploadAsync(
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new InitiateMultipartUploadRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            ContentType = contentType
        };

        var response = await _s3Client.InitiateMultipartUploadAsync(request, cancellationToken);
        return response.UploadId;
    }

    public Task<IReadOnlyList<PresignedUrlInfo>> GeneratePresignedUrlsAsync(
        string objectKey,
        string uploadId,
        int totalParts,
        CancellationToken cancellationToken = default)
    {
        var urls = new List<PresignedUrlInfo>();

        for (var partNumber = 1; partNumber <= totalParts; partNumber++)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(_options.PresignedUrlExpirationMinutes),
                PartNumber = partNumber,
                UploadId = uploadId
            };

            var url = _s3Client.GetPreSignedURL(request);
            urls.Add(new PresignedUrlInfo(partNumber, url));
        }

        return Task.FromResult<IReadOnlyList<PresignedUrlInfo>>(urls);
    }

    public async Task CompleteMultipartUploadAsync(
        string objectKey,
        string uploadId,
        IEnumerable<PartInfo> parts,
        CancellationToken cancellationToken = default)
    {
        var request = new CompleteMultipartUploadRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            UploadId = uploadId,
            PartETags = parts.Select(p => new PartETag(p.PartNumber, p.ETag)).ToList()
        };

        await _s3Client.CompleteMultipartUploadAsync(request, cancellationToken);
    }

    public async Task AbortMultipartUploadAsync(
        string objectKey,
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        var request = new AbortMultipartUploadRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            UploadId = uploadId
        };

        await _s3Client.AbortMultipartUploadAsync(request, cancellationToken);
    }

    public async Task DeleteObjectAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);
    }
}
