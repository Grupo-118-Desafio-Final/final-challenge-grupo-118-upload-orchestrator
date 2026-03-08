using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Application.DTOs;

[ExcludeFromCodeCoverage]
public record UploadResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? CompletedAt);
