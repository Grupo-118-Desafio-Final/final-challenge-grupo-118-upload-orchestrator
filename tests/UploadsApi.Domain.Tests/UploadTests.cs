using FluentAssertions;
using MongoDB.Bson;
using UploadsApi.Domain.Entities;
using UploadsApi.Domain.Enums;

namespace UploadsApi.Domain.Tests;

public class UploadTests
{
    [Fact]
    public void Create_ShouldCreateUploadWithPendingStatus()
    {
        // Arrange
        var userId = "user-123";
        var fileName = "test-video.mp4";
        var contentType = "video/mp4";
        var fileSize = 1024L * 1024 * 100;
        var totalParts = 10;

        // Act
        var upload = Upload.Create(userId, fileName, contentType, fileSize, totalParts);

        // Assert
        upload.Id.Should().NotBe(ObjectId.Empty);
        upload.UserId.Should().Be(userId);
        upload.FileName.Should().Be(fileName);
        upload.ContentType.Should().Be(contentType);
        upload.FileSize.Should().Be(fileSize);
        upload.TotalParts.Should().Be(totalParts);
        upload.Status.Should().Be(UploadStatus.Pending);
        upload.ObjectKey.Should().Contain(userId);
        upload.ObjectKey.Should().Contain(fileName);
        upload.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void StartUploading_WhenPending_ShouldChangeStatusToUploading()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);

        // Act
        upload.StartUploading();

        // Assert
        upload.Status.Should().Be(UploadStatus.Uploading);
        upload.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void StartUploading_WhenNotPending_ShouldThrowException()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);
        upload.StartUploading();

        // Act
        var act = () => upload.StartUploading();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot start uploading from status Uploading");
    }

    [Fact]
    public void Complete_WhenUploading_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);
        upload.StartUploading();

        // Act
        upload.Complete();

        // Assert
        upload.Status.Should().Be(UploadStatus.Completed);
        upload.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_WhenNotUploading_ShouldThrowException()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);

        // Act
        var act = () => upload.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot complete upload from status Pending");
    }

    [Fact]
    public void MarkAsFailed_ShouldSetStatusToFailedAndRecordError()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);
        var errorMessage = "Upload failed due to network error";

        // Act
        upload.MarkAsFailed(errorMessage);

        // Assert
        upload.Status.Should().Be(UploadStatus.Failed);
        upload.ErrorMessage.Should().Be(errorMessage);
        upload.UpdatedAt.Should().NotBeNull();
        upload.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadyUploading_ShouldSetStatusToFailed()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);
        upload.StartUploading();

        // Act
        upload.MarkAsFailed("Network error");

        // Assert
        upload.Status.Should().Be(UploadStatus.Failed);
    }

    [Fact]
    public void Complete_WhenUploading_ShouldSetUpdatedAt()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);
        upload.StartUploading();

        // Act
        upload.Complete();

        // Assert
        upload.UpdatedAt.Should().NotBeNull();
        upload.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        upload.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var upload1 = Upload.Create("user-1", "video1.mp4", "video/mp4", 1024, 1);
        var upload2 = Upload.Create("user-2", "video2.mp4", "video/mp4", 2048, 2);

        // Assert
        upload1.Id.Should().NotBe(upload2.Id);
    }

    [Fact]
    public void Create_ShouldNotSetUpdatedAtOrCompletedAt()
    {
        // Act
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);

        // Assert
        upload.UpdatedAt.Should().BeNull();
        upload.CompletedAt.Should().BeNull();
        upload.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void StartUploading_WhenPending_ShouldSetUpdatedAt()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);

        // Act
        upload.StartUploading();

        // Assert
        upload.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
