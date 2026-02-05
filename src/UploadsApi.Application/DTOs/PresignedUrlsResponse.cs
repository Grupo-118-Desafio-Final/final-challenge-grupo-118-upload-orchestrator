using UploadsApi.Application.Interfaces;

namespace UploadsApi.Application.DTOs;

public record PresignedUrlsResponse(
    Guid UploadId,
    IReadOnlyList<PresignedUrlInfo> Urls);
