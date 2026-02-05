using Amazon.S3;
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
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddSingleton<MongoDbContext>();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var s3Options = configuration.GetSection(S3Options.SectionName).Get<S3Options>()!;

            var config = new AmazonS3Config
            {
                ServiceURL = s3Options.ServiceUrl,
                ForcePathStyle = s3Options.ForcePathStyle
            };

            return new AmazonS3Client(
                s3Options.AccessKey,
                s3Options.SecretKey,
                config);
        });

        services.AddScoped<IUploadRepository, MongoDbUploadRepository>();
        services.AddScoped<IStorageService, S3StorageService>();
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        return services;
    }
}
