namespace UploadsApi.Infrastructure.Options;

public class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "video-uploads";
    public int SasExpirationMinutes { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Public endpoint for browser access to blob storage.
    /// Used to rewrite SAS URLs for external clients (e.g., http://localhost:10000/devstoreaccount1).
    /// If empty, uses the endpoint from ConnectionString.
    /// </summary>
    public string PublicBlobEndpoint { get; set; } = string.Empty;
}
