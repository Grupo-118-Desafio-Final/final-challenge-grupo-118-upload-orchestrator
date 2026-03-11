using MongoDB.Bson;
using UploadsApi.Application.DTOs;
using UploadsApi.Application.Interfaces;
using UploadsApi.Domain.Entities;
using UploadsApi.Domain.Enums;
using UploadsApi.Domain.Events;

namespace UploadsApi.Application.Services;

public class UploadService : IUploadService
{
    private readonly IUploadRepository _uploadRepository;
    private readonly IStorageService _storageService;
    private readonly IMessagePublisher _messagePublisher;

    public UploadService(
        IUploadRepository uploadRepository,
        IStorageService storageService,
        IMessagePublisher messagePublisher)
    {
        _uploadRepository = uploadRepository;
        _storageService = storageService;
        _messagePublisher = messagePublisher;
    }

    public async Task<CreateUploadResponse> CreateUploadAsync(
        string userId,
        CreateUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var upload = Upload.Create(
            userId,
            request.FileName,
            request.ContentType,
            request.FileSize,
            request.TotalParts);

        await _uploadRepository.AddAsync(upload, cancellationToken);

        return new CreateUploadResponse(
            upload.Id.ToString(),
            upload.Status.ToString());
    }

    public async Task<PresignedUrlsResponse> GetPresignedUrlsAsync(
        string uploadId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(uploadId, out var objectId))
        {
            throw new ArgumentException($"Invalid upload ID format: {uploadId}", nameof(uploadId));
        }

        var upload = await _uploadRepository.GetByIdAndUserIdAsync(objectId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Upload with ID {uploadId} not found");

        if (upload.Status != UploadStatus.Pending && upload.Status != UploadStatus.Uploading)
        {
            throw new InvalidOperationException($"Cannot get presigned URLs for upload with status {upload.Status}");
        }

        if (upload.Status == UploadStatus.Pending)
        {
            upload.StartUploading();
            await _uploadRepository.UpdateAsync(upload, cancellationToken);
        }

        var urls = await _storageService.GeneratePresignedUrlsAsync(
            upload.ObjectKey,
            upload.TotalParts,
            cancellationToken);

        return new PresignedUrlsResponse(upload.Id.ToString(), urls);
    }

    public async Task CompleteUploadAsync(
        string uploadId,
        string userId,
        string planId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(uploadId, out var objectId))
        {
            throw new ArgumentException($"Invalid upload ID format: {uploadId}", nameof(uploadId));
        }

        var upload = await _uploadRepository.GetByIdAndUserIdAsync(objectId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Upload with ID {uploadId} not found");

        if (upload.Status != UploadStatus.Uploading)
        {
            throw new InvalidOperationException($"Cannot complete upload with status {upload.Status}");
        }

        try
        {
            await _storageService.CommitUploadAsync(
                upload.ObjectKey,
                upload.TotalParts,
                cancellationToken);

            upload.Complete();
            await _uploadRepository.UpdateAsync(upload, cancellationToken);

            var processing = Processing.Create(userId, upload.ObjectKey);
            var blobUrl = _storageService.GetBlobUrl(upload.ObjectKey);

            var videoUploadedEvent = new VideoUploadedEvent(
                userId,
                planId,
                upload.Id.ToString(),
                blobUrl,
                DateTime.UtcNow);

            await _messagePublisher.PublishAsync(videoUploadedEvent, "video.uploaded", cancellationToken);
        }
        catch (Exception ex)
        {
            upload.MarkAsFailed(ex.Message);
            await _uploadRepository.UpdateAsync(upload, cancellationToken);

            var uploadFailedEvent = new UploadFailedEvent(
                upload.Id.ToString(),
                upload.UserId,
                upload.FileName,
                ex.Message,
                DateTime.UtcNow);

            await _messagePublisher.PublishAsync(uploadFailedEvent, "upload.failed", cancellationToken);

            throw;
        }
    }

    public async Task<UploadResponse?> GetUploadAsync(
        string uploadId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(uploadId, out var objectId))
        {
            throw new ArgumentException($"Invalid upload ID format: {uploadId}", nameof(uploadId));
        }

        var upload = await _uploadRepository.GetByIdAndUserIdAsync(objectId, userId, cancellationToken);

        return upload is null ? null : MapToResponse(upload);
    }

    public async Task<PagedResponse<UploadResponse>> GetUploadsAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _uploadRepository.GetByUserIdAsync(
            userId,
            page,
            pageSize,
            cancellationToken);

        var responses = items.Select(MapToResponse).ToList();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PagedResponse<UploadResponse>(
            responses,
            page,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task AbortUploadAsync(
        string uploadId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(uploadId, out var objectId))
        {
            throw new ArgumentException($"Invalid upload ID format: {uploadId}", nameof(uploadId));
        }

        var upload = await _uploadRepository.GetByIdAndUserIdAsync(objectId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Upload with ID {uploadId} not found");

        if (upload.Status == UploadStatus.Completed || upload.Status == UploadStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot abort upload with status {upload.Status}");
        }

        await _storageService.DeleteAsync(upload.ObjectKey, cancellationToken);
        await _uploadRepository.DeleteAsync(upload, cancellationToken);
    }

    private static UploadResponse MapToResponse(Upload upload)
    {
        return new UploadResponse(
            upload.Id.ToString(),
            upload.FileName,
            upload.ContentType,
            upload.FileSize,
            upload.Status.ToString(),
            upload.ErrorMessage,
            upload.CreatedAt,
            upload.CompletedAt);
    }
}
