using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Application.Interfaces;

public interface IStorageService
{
    Task<IReadOnlyList<PresignedUrlInfo>> GeneratePresignedUrlsAsync(
        string objectKey,
        int totalParts,
        CancellationToken cancellationToken = default);

    Task CommitUploadAsync(
        string objectKey,
        int totalParts,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    string GetBlobUrl(string objectKey);
}

[ExcludeFromCodeCoverage]
public record PresignedUrlInfo(int PartNumber, string Url);
