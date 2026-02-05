namespace UploadsApi.Domain.Events;

public record VideoUploadedEvent(
    Guid UploadId,
    string UserId,
    string ObjectKey,
    string FileName,
    string ContentType,
    long FileSize,
    DateTime UploadedAt);
