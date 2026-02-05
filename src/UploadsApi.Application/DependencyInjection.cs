using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UploadsApi.Application.Services;
using UploadsApi.Application.Validators;

namespace UploadsApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUploadService, UploadService>();
        services.AddValidatorsFromAssemblyContaining<CreateUploadRequestValidator>();

        return services;
    }
}
