using MongoDB.Bson;
using UploadsApi.Domain.Enums;

namespace UploadsApi.Domain.Entities;

public class Upload
{
    public ObjectId Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public UploadStatus Status { get; private set; }
    public int TotalParts { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public string ZipBlobUrl { get; private set; } = string.Empty;

    public ProcessingStatus ProcessingStatus { get; private set; } = ProcessingStatus.NotStarted;

    private Upload()
    {
    }

    public static Upload Create(
        string userId,
        string fileName,
        string contentType,
        long fileSize,
        int totalParts)
    {
        var upload = new Upload
        {
            Id = ObjectId.GenerateNewId(),
            UserId = userId,
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            TotalParts = totalParts,
            Status = UploadStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessingStatus =  ProcessingStatus.NotStarted
        };

        var idString = upload.Id.ToString();
        upload.ObjectKey = $"uploads/{userId}/{upload.CreatedAt:yyyyMMddHHmmss}_{idString}_{fileName}";

        return upload;
    }

    public void StartUploading()
    {
        if (Status != UploadStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start uploading from status {Status}");
        }

        Status = UploadStatus.Uploading;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != UploadStatus.Uploading)
        {
            throw new InvalidOperationException($"Cannot complete upload from status {Status}");
        }

        Status = UploadStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = UploadStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }
}