using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Domain.Events;

[ExcludeFromCodeCoverage]
public record VideoUploadedEvent(
    string UserId,
    string PlanId,
    Guid ProcessingId,
    string BlobUrl,
    DateTime EventAt);
