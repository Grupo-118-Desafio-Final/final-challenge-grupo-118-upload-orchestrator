namespace UploadsApi.Domain.Events;

public record VideoUploadedEvent(
    string UserId,
    string PlanId,
    Guid ProcessingId,
    string BlobUrl,
    DateTime EventAt);
