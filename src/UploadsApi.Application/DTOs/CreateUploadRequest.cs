using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Application.DTOs;

[ExcludeFromCodeCoverage]
public record CreateUploadRequest(
    string FileName,
    string ContentType,
    long FileSize,
    int TotalParts);
