using System.Diagnostics.CodeAnalysis;
using UploadsApi.Application.Interfaces;

namespace UploadsApi.Application.DTOs;

[ExcludeFromCodeCoverage]
public record PresignedUrlsResponse(
    Guid UploadId,
    IReadOnlyList<PresignedUrlInfo> Urls);
