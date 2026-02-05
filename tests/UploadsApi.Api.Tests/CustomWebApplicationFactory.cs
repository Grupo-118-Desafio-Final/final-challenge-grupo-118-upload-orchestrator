using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NSubstitute;
using UploadsApi.Application.Interfaces;
using UploadsApi.Domain.Entities;
using UploadsApi.Infrastructure.Persistence;

namespace UploadsApi.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IUploadRepository UploadRepository { get; } = Substitute.For<IUploadRepository>();
    public IStorageService StorageService { get; } = Substitute.For<IStorageService>();
    public IMessagePublisher MessagePublisher { get; } = Substitute.For<IMessagePublisher>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove existing MongoDB context registration
            RemoveService<MongoDbContext>(services);

            // Remove existing registrations
            RemoveService<IUploadRepository>(services);
            RemoveService<IStorageService>(services);
            RemoveService<IMessagePublisher>(services);

            // Register test MongoDbContext that doesn't connect to MongoDB
            services.AddSingleton<MongoDbContext, TestMongoDbContext>();

            // Register mocks
            services.AddScoped(_ => UploadRepository);
            services.AddScoped(_ => StorageService);
            services.AddSingleton(_ => MessagePublisher);
        });
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }
}

internal class TestMongoDbContext : MongoDbContext
{
    public TestMongoDbContext() : base()
    {
    }

    public override IMongoCollection<Upload> Uploads => Substitute.For<IMongoCollection<Upload>>();

    public override Task EnsureIndexesCreatedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
