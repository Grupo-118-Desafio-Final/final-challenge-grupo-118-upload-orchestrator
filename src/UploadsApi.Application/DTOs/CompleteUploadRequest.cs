using UploadsApi.Application.Interfaces;

namespace UploadsApi.Application.DTOs;

public record CompleteUploadRequest(IReadOnlyList<PartInfo> Parts);
