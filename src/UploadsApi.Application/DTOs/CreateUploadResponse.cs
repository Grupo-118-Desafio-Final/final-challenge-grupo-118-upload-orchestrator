using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Application.DTOs;

[ExcludeFromCodeCoverage]
public record CreateUploadResponse(
    string UploadId,
    string Status);
