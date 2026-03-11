using UploadsApi.Application.DTOs;

namespace UploadsApi.Application.Services;

public interface IUploadService
{
    Task<CreateUploadResponse> CreateUploadAsync(
        string userId,
        CreateUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<PresignedUrlsResponse> GetPresignedUrlsAsync(
        string uploadId,
        string userId,
        CancellationToken cancellationToken = default);

    Task CompleteUploadAsync(
        string uploadId,
        string userId,
        string planId,
        CancellationToken cancellationToken = default);

    Task<UploadResponse?> GetUploadAsync(
        string uploadId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<UploadResponse>> GetUploadsAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AbortUploadAsync(
        string uploadId,
        string userId,
        CancellationToken cancellationToken = default);
}
