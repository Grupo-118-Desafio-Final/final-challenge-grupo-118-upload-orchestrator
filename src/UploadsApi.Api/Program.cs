using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key configuration is required");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

/// <summary>
/// 
/// </summary>
public static partial class Program
{
}