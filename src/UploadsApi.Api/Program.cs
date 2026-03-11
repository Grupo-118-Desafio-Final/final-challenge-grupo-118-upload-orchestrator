using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using StandardDependencies.Injection;
using StandardDependencies.Models;
using UploadsApi.Api.Middleware;
using UploadsApi.Application;
using UploadsApi.Infrastructure;
using UploadsApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var swaggerOptions = builder
    .Configuration
    .GetSection(SwaggerOptions.SectionName)
    .Get<SwaggerOptions>();

var openTelemetryOptions = builder
    .Configuration
    .GetSection(OpenTelemetryOptions.SectionName)
    .Get<OpenTelemetryOptions>();

// Configura elementos comuns: Environment Variables, OpenTelemetry e Swagger
builder.ConfigureCommonElements(openTelemetryOptions, swaggerOptions);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("UserId", new OpenApiSecurityScheme
    {
        Description = "User ID passed from API Gateway",
        Name = "X-User-Id",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "UserId"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddMongoDb(
        _ => new MongoClient(builder.Configuration["MongoDb:ConnectionString"]),
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "db", "mongo" })
    .AddRabbitMQ(
        rabbitConnectionString: builder.Configuration["RabbitMQ:Uri"]!,
        name: "rabbitmq");

var app = builder.Build();

app.UseStandarizedSwagger(swaggerOptions);

var mongoDbContext = app.Services.GetRequiredService<MongoDbContext>();
await mongoDbContext.EnsureIndexesCreatedAsync();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

/// <summary>
/// 
/// </summary>
public static partial class Program
{
}