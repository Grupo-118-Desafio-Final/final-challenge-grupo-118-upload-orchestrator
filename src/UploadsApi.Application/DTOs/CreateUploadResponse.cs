namespace UploadsApi.Application.DTOs;

public record CreateUploadResponse(
    Guid UploadId,
    string ObjectKey,
    string Status);
