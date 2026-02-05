namespace UploadsApi.Application.DTOs;

public record CreateUploadRequest(
    string FileName,
    string ContentType,
    long FileSize,
    int TotalParts);
