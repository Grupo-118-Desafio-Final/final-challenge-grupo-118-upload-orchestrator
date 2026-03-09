using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UploadsApi.Domain.Entities;
using UploadsApi.Infrastructure.Options;
using UploadsApi.Infrastructure.Persistence.Mappings;

namespace UploadsApi.Infrastructure.Persistence;

[ExcludeFromCodeCoverage]
public class MongoDbContext
{
    private readonly IMongoDatabase? _database;

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        UploadClassMap.Register();

        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    protected MongoDbContext()
    {
        // For testing purposes
    }

    public virtual IMongoCollection<Upload> Uploads => _database!.GetCollection<Upload>("uploads");

    public virtual async Task EnsureIndexesCreatedAsync(CancellationToken cancellationToken = default)
    {
        var indexModels = new List<CreateIndexModel<Upload>>
        {
            new(
                Builders<Upload>.IndexKeys.Ascending(x => x.UserId),
                new CreateIndexOptions { Name = "idx_userId" }),
            new(
                Builders<Upload>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Ascending(x => x.Status),
                new CreateIndexOptions { Name = "idx_userId_status" }),
            new(
                Builders<Upload>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "idx_userId_createdAt" })
        };

        await Uploads.Indexes.CreateManyAsync(indexModels, cancellationToken);
    }
}
