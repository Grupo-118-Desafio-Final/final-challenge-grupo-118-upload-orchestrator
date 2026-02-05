using UploadsApi.Domain.Enums;

namespace UploadsApi.Domain.Entities;

public class Upload
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string? MultipartUploadId { get; private set; }
    public UploadStatus Status { get; private set; }
    public int TotalParts { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Upload() { }

    public static Upload Create(
        string userId,
        string fileName,
        string contentType,
        long fileSize,
        int totalParts)
    {
        var upload = new Upload
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            TotalParts = totalParts,
            Status = UploadStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        upload.ObjectKey = $"uploads/{userId}/{upload.CreatedAt:yyyyMMddHHmmss}_{upload.Id:N}_{fileName}";

        return upload;
    }

    public void SetMultipartUploadId(string multipartUploadId)
    {
        MultipartUploadId = multipartUploadId;
        UpdatedAt = DateTime.UtcNow;
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

    public void StartProcessing()
    {
        if (Status != UploadStatus.Uploading)
        {
            throw new InvalidOperationException($"Cannot start processing from status {Status}");
        }

        Status = UploadStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted()
    {
        if (Status != UploadStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot mark as completed from status {Status}");
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
