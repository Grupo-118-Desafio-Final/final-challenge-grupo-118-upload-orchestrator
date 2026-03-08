using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Application.DTOs;

[ExcludeFromCodeCoverage]
public record CreateUploadResponse(
    Guid UploadId,
    string Status);
