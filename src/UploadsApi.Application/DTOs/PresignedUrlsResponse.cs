using System.Diagnostics.CodeAnalysis;
using UploadsApi.Application.Interfaces;

namespace UploadsApi.Application.DTOs;

[ExcludeFromCodeCoverage]
public record PresignedUrlsResponse(
    string UploadId,
    IReadOnlyList<PresignedUrlInfo> Urls);
