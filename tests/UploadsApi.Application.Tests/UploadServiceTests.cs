using FluentAssertions;
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
        var uploadId = Guid.NewGuid();
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 2);

        var expectedUrls = new List<PresignedUrlInfo>
        {
            new(1, "https://example.com/part1"),
            new(2, "https://example.com/part2")
        };

        _uploadRepository.GetByIdAndUserIdAsync(uploadId, userId, Arg.Any<CancellationToken>())
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
        var uploadId = Guid.NewGuid();
        var userId = "user-123";

        _uploadRepository.GetByIdAndUserIdAsync(uploadId, userId, Arg.Any<CancellationToken>())
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
        var uploadId = Guid.NewGuid();
        var userId = "user-123";
        var planId = "plan-456";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 2);
        upload.StartUploading();

        _uploadRepository.GetByIdAndUserIdAsync(uploadId, userId, Arg.Any<CancellationToken>())
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
                e.ProcessingId != Guid.Empty &&
                e.BlobUrl == "https://storage.example.com/blob"),
            "video.uploaded",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteUploadAsync_WhenNotUploading_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var uploadId = Guid.NewGuid();
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 2);

        _uploadRepository.GetByIdAndUserIdAsync(uploadId, userId, Arg.Any<CancellationToken>())
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
        var uploadId = Guid.NewGuid();
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 1);

        _uploadRepository.GetByIdAndUserIdAsync(uploadId, userId, Arg.Any<CancellationToken>())
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
        var uploadId = Guid.NewGuid();
        var userId = "user-123";

        _uploadRepository.GetByIdAndUserIdAsync(uploadId, userId, Arg.Any<CancellationToken>())
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
        var uploadId = Guid.NewGuid();
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 1);

        _uploadRepository.GetByIdAndUserIdAsync(uploadId, userId, Arg.Any<CancellationToken>())
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
        var uploadId = Guid.NewGuid();
        var userId = "user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 1);
        upload.StartUploading();
        upload.Complete();

        _uploadRepository.GetByIdAndUserIdAsync(uploadId, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        var act = () => _sut.AbortUploadAsync(uploadId, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot abort upload with status Completed");
    }
}
