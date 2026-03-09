using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Infrastructure.Options;

[ExcludeFromCodeCoverage]
public class MongoDbOptions
{
    public const string SectionName = "MongoDB";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
