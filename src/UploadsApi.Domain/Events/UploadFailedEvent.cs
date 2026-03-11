using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Domain.Events;

[ExcludeFromCodeCoverage]
public record UploadFailedEvent(
    string UploadId,
    string UserId,
    string FileName,
    string ErrorMessage,
    DateTime FailedAt);
