using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Application.DTOs;

[ExcludeFromCodeCoverage]
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
