namespace UploadsApi.Domain.Events;

public record UploadFailedEvent(
    Guid UploadId,
    string UserId,
    string FileName,
    string ErrorMessage,
    DateTime FailedAt);
