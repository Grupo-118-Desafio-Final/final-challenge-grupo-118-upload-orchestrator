using MongoDB.Bson;
using UploadsApi.Domain.Entities;

namespace UploadsApi.Application.Interfaces;

public interface IUploadRepository
{
    Task<Upload?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default);
    Task<Upload?> GetByIdAndUserIdAsync(ObjectId id, string userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Upload> Items, int TotalCount)> GetByUserIdAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Upload upload, CancellationToken cancellationToken = default);
    Task UpdateAsync(Upload upload, CancellationToken cancellationToken = default);
    Task DeleteAsync(Upload upload, CancellationToken cancellationToken = default);
}
