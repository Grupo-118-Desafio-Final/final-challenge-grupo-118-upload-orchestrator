namespace UploadsApi.Application.Interfaces;

public interface IStorageService
{
    Task<string> InitiateMultipartUploadAsync(
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PresignedUrlInfo>> GeneratePresignedUrlsAsync(
        string objectKey,
        string uploadId,
        int totalParts,
        CancellationToken cancellationToken = default);

    Task CompleteMultipartUploadAsync(
        string objectKey,
        string uploadId,
        IEnumerable<PartInfo> parts,
        CancellationToken cancellationToken = default);

    Task AbortMultipartUploadAsync(
        string objectKey,
        string uploadId,
        CancellationToken cancellationToken = default);

    Task DeleteObjectAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}

public record PresignedUrlInfo(int PartNumber, string Url);

public record PartInfo(int PartNumber, string ETag);
