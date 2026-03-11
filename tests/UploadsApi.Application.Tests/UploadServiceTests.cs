using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using UploadsApi.Application.DTOs;
using UploadsApi.Application.Interfaces;
using UploadsApi.Application.Services;
using UploadsApi.Domain.Entities;
using UploadsApi.Domain.Enums;
using UploadsApi.Domain.Events;

namespace UploadsApi.Application.Tests;

public class UploadServiceTests
{
    private readonly IUploadRepository _uploadRepository;
    private readonly IStorageService _storageService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly UploadService _sut;

    public UploadServiceTests()
    {
        _uploadRepository = Substitute.For<IUploadRepository>();
        _storageService = Substitute.For<IStorageService>();
        _messagePublisher = Substitute.For<IMessagePublisher>();
        _sut = new UploadService(_uploadRepository, _storageService, _messagePublisher);
    }

    [Fact]
    public async Task CreateUploadAsync_ShouldCreateUploadAndReturnResponse()
    {
        // Arrange
        var userId = "user-123";
        var request = new CreateUploadRequest("test.mp4", "video/mp4", 1024 * 1024, 1);

        // Act
        var result = await _sut.CreateUploadAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.UploadId.Should().NotBeEmpty();
        result.Status.Should().Be("Pending");

        await _uploadRepository.Received(1).AddAsync(Arg.Any<Upload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPresignedUrlsAsync_WhenUploadExists_ShouldReturnUrls()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 2);
        var uploadId = upload.Id.ToString();
        var userId = "user-123";

        var expectedUrls = new List<PresignedUrlInfo>
        {
            new(1, "https://example.com/part1"),
            new(2, "https://example.com/part2")
        };

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        _storageService.GeneratePresignedUrlsAsync(
            Arg.Any<string>(),
            2,
            Arg.Any<CancellationToken>())
            .Returns(expectedUrls);

        // Act
        var result = await _sut.GetPresignedUrlsAsync(uploadId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Urls.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPresignedUrlsAsync_WhenUploadNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var uploadId = ObjectId.GenerateNewId().ToString();
        var userId = "user-123";

        _uploadRepository.GetByIdAndUserIdAsync(Arg.Any<ObjectId>(), userId, Arg.Any<CancellationToken>())
            .Returns((Upload?)null);

        // Act
        var act = () => _sut.GetPresignedUrlsAsync(uploadId, userId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CompleteUploadAsync_WhenValid_ShouldCompleteAndPublishEvent()
    {
        // Arrange
        var userId = "user-123";
        var planId = "plan-456";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 2);
        var uploadId = upload.Id.ToString();
        upload.StartUploading();

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        _storageService.GetBlobUrl(Arg.Any<string>())
            .Returns("https://storage.example.com/blob");

        // Act
        await _sut.CompleteUploadAsync(uploadId, userId, planId);

        // Assert
        await _storageService.Received(1).CommitUploadAsync(
            Arg.Any<string>(),
            2,
            Arg.Any<CancellationToken>());

        await _messagePublisher.Received(1).PublishAsync(
            Arg.Is<VideoUploadedEvent>(e =>
                e.UserId == userId &&
                e.PlanId == planId &&
                !string.IsNullOrEmpty(e.ProcessingId) &&
                e.BlobUrl == "https://storage.example.com/blob"),
            "video.uploaded",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteUploadAsync_WhenNotUploading_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 2);
        var uploadId = upload.Id.ToString();

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        var act = () => _sut.CompleteUploadAsync(uploadId, userId, "plan-456");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot complete upload with status Pending");
    }

    [Fact]
    public async Task GetUploadAsync_WhenExists_ShouldReturnUploadResponse()
    {
        // Arrange
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 1);
        var uploadId = upload.Id.ToString();

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        var result = await _sut.GetUploadAsync(uploadId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("test.mp4");
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetUploadAsync_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        var uploadId = ObjectId.GenerateNewId().ToString();
        var userId = "user-123";

        _uploadRepository.GetByIdAndUserIdAsync(Arg.Any<ObjectId>(), userId, Arg.Any<CancellationToken>())
            .Returns((Upload?)null);

        // Act
        var result = await _sut.GetUploadAsync(uploadId, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadsAsync_ShouldReturnPagedResponse()
    {
        // Arrange
        var userId = "user-123";
        var uploads = new List<Upload>
        {
            Upload.Create(userId, "test1.mp4", "video/mp4", 1024, 1),
            Upload.Create(userId, "test2.mp4", "video/mp4", 2048, 1)
        };

        _uploadRepository.GetByUserIdAsync(userId, 1, 10, Arg.Any<CancellationToken>())
            .Returns((uploads, 2));

        // Act
        var result = await _sut.GetUploadsAsync(userId, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task AbortUploadAsync_WhenPending_ShouldAbortAndDelete()
    {
        // Arrange
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 1);
        var uploadId = upload.Id.ToString();

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        await _sut.AbortUploadAsync(uploadId, userId);

        // Assert
        await _storageService.Received(1).DeleteAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await _uploadRepository.Received(1).DeleteAsync(upload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AbortUploadAsync_WhenCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 1);
        var uploadId = upload.Id.ToString();
        upload.StartUploading();
        upload.Complete();

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        var act = () => _sut.AbortUploadAsync(uploadId, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot abort upload with status Completed");
    }

    [Fact]
    public async Task GetPresignedUrlsAsync_WithInvalidUploadId_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidUploadId = "invalid-id";
        var userId = "user-123";

        // Act
        var act = () => _sut.GetPresignedUrlsAsync(invalidUploadId, userId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid upload ID format: invalid-id*")
            .WithParameterName("uploadId");
    }

    [Fact]
    public async Task GetPresignedUrlsAsync_WhenStatusIsCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 2);
        var uploadId = upload.Id.ToString();
        upload.StartUploading();
        upload.Complete();

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        var act = () => _sut.GetPresignedUrlsAsync(uploadId, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot get presigned URLs for upload with status Completed");
    }

    [Fact]
    public async Task GetPresignedUrlsAsync_WhenStatusIsUploading_ShouldNotUpdateStatus()
    {
        // Arrange
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 2);
        var uploadId = upload.Id.ToString();
        upload.StartUploading();

        var expectedUrls = new List<PresignedUrlInfo>
        {
            new(1, "https://example.com/part1"),
            new(2, "https://example.com/part2")
        };

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        _storageService.GeneratePresignedUrlsAsync(
            Arg.Any<string>(),
            2,
            Arg.Any<CancellationToken>())
            .Returns(expectedUrls);

        // Act
        var result = await _sut.GetPresignedUrlsAsync(uploadId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Urls.Should().HaveCount(2);
        await _uploadRepository.DidNotReceive().UpdateAsync(Arg.Any<Upload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteUploadAsync_WithInvalidUploadId_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidUploadId = "invalid-id";
        var userId = "user-123";

        // Act
        var act = () => _sut.CompleteUploadAsync(invalidUploadId, userId, "plan-123");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid upload ID format: invalid-id*")
            .WithParameterName("uploadId");
    }

    [Fact]
    public async Task CompleteUploadAsync_WhenUploadNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var uploadId = ObjectId.GenerateNewId().ToString();
        var userId = "user-123";

        _uploadRepository.GetByIdAndUserIdAsync(Arg.Any<ObjectId>(), userId, Arg.Any<CancellationToken>())
            .Returns((Upload?)null);

        // Act
        var act = () => _sut.CompleteUploadAsync(uploadId, userId, "plan-123");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CompleteUploadAsync_WhenCommitFails_ShouldMarkAsFailedAndPublishFailedEvent()
    {
        // Arrange
        var userId = "user-123";
        var planId = "plan-456";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 2);
        var uploadId = upload.Id.ToString();
        upload.StartUploading();

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        var exception = new Exception("Storage commit failed");
        _storageService.CommitUploadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));

        // Act
        var act = () => _sut.CompleteUploadAsync(uploadId, userId, planId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Storage commit failed");

        await _uploadRepository.Received(1).UpdateAsync(
            Arg.Is<Upload>(u => u.Status == UploadStatus.Failed && u.ErrorMessage == "Storage commit failed"),
            Arg.Any<CancellationToken>());

        await _messagePublisher.Received(1).PublishAsync(
            Arg.Is<UploadFailedEvent>((UploadFailedEvent e) =>
                e.UploadId == uploadId &&
                e.UserId == userId &&
                e.FileName == "test.mp4" &&
                e.ErrorMessage == "Storage commit failed"),
            "upload.failed",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUploadAsync_WithInvalidUploadId_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidUploadId = "invalid-id";
        var userId = "user-123";

        // Act
        var act = () => _sut.GetUploadAsync(invalidUploadId, userId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid upload ID format: invalid-id*")
            .WithParameterName("uploadId");
    }

    [Fact]
    public async Task AbortUploadAsync_WithInvalidUploadId_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidUploadId = "invalid-id";
        var userId = "user-123";

        // Act
        var act = () => _sut.AbortUploadAsync(invalidUploadId, userId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid upload ID format: invalid-id*")
            .WithParameterName("uploadId");
    }

    [Fact]
    public async Task AbortUploadAsync_WhenNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var uploadId = ObjectId.GenerateNewId().ToString();
        var userId = "user-123";

        _uploadRepository.GetByIdAndUserIdAsync(Arg.Any<ObjectId>(), userId, Arg.Any<CancellationToken>())
            .Returns((Upload?)null);

        // Act
        var act = () => _sut.AbortUploadAsync(uploadId, userId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AbortUploadAsync_WhenFailed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 1);
        var uploadId = upload.Id.ToString();
        upload.MarkAsFailed("Some error");

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        var act = () => _sut.AbortUploadAsync(uploadId, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot abort upload with status Failed");
    }

    [Fact]
    public async Task AbortUploadAsync_WhenUploading_ShouldAbortAndDelete()
    {
        // Arrange
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 1);
        var uploadId = upload.Id.ToString();
        upload.StartUploading();

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        await _sut.AbortUploadAsync(uploadId, userId);

        // Assert
        await _storageService.Received(1).DeleteAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await _uploadRepository.Received(1).DeleteAsync(upload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUploadsAsync_WhenNoUploads_ShouldReturnEmptyPagedResponse()
    {
        // Arrange
        var userId = "user-123";
        var emptyList = new List<Upload>();

        _uploadRepository.GetByUserIdAsync(userId, 1, 10, Arg.Any<CancellationToken>())
            .Returns((emptyList, 0));

        // Act
        var result = await _sut.GetUploadsAsync(userId, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetUploadsAsync_WithMultiplePages_ShouldCalculateTotalPages()
    {
        // Arrange
        var userId = "user-123";
        var uploads = new List<Upload>
        {
            Upload.Create(userId, "test1.mp4", "video/mp4", 1024, 1),
            Upload.Create(userId, "test2.mp4", "video/mp4", 2048, 1)
        };

        _uploadRepository.GetByUserIdAsync(userId, 1, 2, Arg.Any<CancellationToken>())
            .Returns((uploads, 5));

        // Act
        var result = await _sut.GetUploadsAsync(userId, 1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetUploadAsync_ShouldMapAllProperties()
    {
        // Arrange
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 2048, 1);
        var uploadId = upload.Id.ToString();
        upload.StartUploading();
        upload.Complete();

        _uploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        var result = await _sut.GetUploadAsync(uploadId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(upload.Id.ToString());
        result.FileName.Should().Be("test.mp4");
        result.ContentType.Should().Be("video/mp4");
        result.FileSize.Should().Be(2048);
        result.Status.Should().Be("Completed");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.CompletedAt.Should().NotBeNull();
        result.ZipBlobUrl.Should().Be(string.Empty);
        result.ProcessingStatus.Should().Be("NotStarted");
    }
}
