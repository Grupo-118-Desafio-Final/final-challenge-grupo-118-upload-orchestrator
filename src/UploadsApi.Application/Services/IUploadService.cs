using UploadsApi.Application.DTOs;

namespace UploadsApi.Application.Services;

public interface IUploadService
{
    Task<CreateUploadResponse> CreateUploadAsync(
        string userId,
        CreateUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<PresignedUrlsResponse> GetPresignedUrlsAsync(
        Guid uploadId,
        string userId,
        CancellationToken cancellationToken = default);

    Task CompleteUploadAsync(
        Guid uploadId,
        string userId,
        string planId,
        CancellationToken cancellationToken = default);

    Task<UploadResponse?> GetUploadAsync(
        Guid uploadId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<UploadResponse>> GetUploadsAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AbortUploadAsync(
        Guid uploadId,
        string userId,
        CancellationToken cancellationToken = default);
}
