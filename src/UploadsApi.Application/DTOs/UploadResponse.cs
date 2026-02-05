namespace UploadsApi.Application.DTOs;

public record UploadResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? CompletedAt);
