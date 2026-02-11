using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UploadsApi.Application.Interfaces;
using UploadsApi.Infrastructure.Messaging;
using UploadsApi.Infrastructure.Options;
using UploadsApi.Infrastructure.Persistence;
using UploadsApi.Infrastructure.Persistence.Repositories;
using UploadsApi.Infrastructure.Storage;

namespace UploadsApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.Configure<AzureBlobOptions>(configuration.GetSection(AzureBlobOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddSingleton<MongoDbContext>();

        services.AddSingleton<BlobServiceClient>(sp =>
        {
            var options = configuration.GetSection(AzureBlobOptions.SectionName).Get<AzureBlobOptions>()!;

            var blobClientOptions = new BlobClientOptions
            {
                Retry =
                {
                    MaxRetries = options.MaxRetries,
                    Delay = TimeSpan.FromSeconds(options.RetryDelaySeconds),
                    Mode = Azure.Core.RetryMode.Exponential
                }
            };

            return new BlobServiceClient(options.ConnectionString, blobClientOptions);
        });

        services.AddScoped<IUploadRepository, MongoDbUploadRepository>();
        services.AddScoped<IStorageService, AzureBlobStorageService>();
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        return services;
    }
}
