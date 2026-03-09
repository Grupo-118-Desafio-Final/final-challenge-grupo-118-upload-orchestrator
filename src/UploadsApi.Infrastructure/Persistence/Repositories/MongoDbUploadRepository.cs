using System.Diagnostics.CodeAnalysis;
using MongoDB.Driver;
using UploadsApi.Application.Interfaces;
using UploadsApi.Domain.Entities;

namespace UploadsApi.Infrastructure.Persistence.Repositories;

[ExcludeFromCodeCoverage]
public class MongoDbUploadRepository : IUploadRepository
{
    private readonly MongoDbContext _context;

    public MongoDbUploadRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Upload?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Upload>.Filter.Eq(x => x.Id, id);
        return await _context.Uploads.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Upload?> GetByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Upload>.Filter.And(
            Builders<Upload>.Filter.Eq(x => x.Id, id),
            Builders<Upload>.Filter.Eq(x => x.UserId, userId));

        return await _context.Uploads.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Upload> Items, int TotalCount)> GetByUserIdAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Upload>.Filter.Eq(x => x.UserId, userId);
        var sort = Builders<Upload>.Sort.Descending(x => x.CreatedAt);

        var totalCount = await _context.Uploads.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await _context.Uploads
            .Find(filter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (items, (int)totalCount);
    }

    public async Task AddAsync(Upload upload, CancellationToken cancellationToken = default)
    {
        await _context.Uploads.InsertOneAsync(upload, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Upload upload, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Upload>.Filter.Eq(x => x.Id, upload.Id);
        await _context.Uploads.ReplaceOneAsync(filter, upload, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Upload upload, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Upload>.Filter.Eq(x => x.Id, upload.Id);
        await _context.Uploads.DeleteOneAsync(filter, cancellationToken);
    }
}
