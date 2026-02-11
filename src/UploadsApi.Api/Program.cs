using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using UploadsApi.Api.Middleware;
using UploadsApi.Application;
using UploadsApi.Infrastructure;
using UploadsApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

#pragma warning disable CS0618 
BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3; 
#pragma warning restore CS0618

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Uploads API",
        Version = "v1",
        Description = "API for video upload orchestration"
    });

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
        builder.Configuration["MongoDB:ConnectionString"]!,
        name: "mongodb")
    .AddRabbitMQ(rabbitConnectionString: builder.Configuration.GetSection("RabbitMQ:ConnectionString").Value
        ?? $"amqp://{builder.Configuration["RabbitMQ:UserName"]}:{builder.Configuration["RabbitMQ:Password"]}@{builder.Configuration["RabbitMQ:HostName"]}:{builder.Configuration["RabbitMQ:Port"]}/",
        name: "rabbitmq");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var mongoDbContext = app.Services.GetRequiredService<MongoDbContext>();
await mongoDbContext.EnsureIndexesCreatedAsync();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
