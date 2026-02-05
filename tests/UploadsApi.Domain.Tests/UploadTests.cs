using FluentAssertions;
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
        var fileSize = 1024L * 1024 * 100; // 100 MB
        var totalParts = 10;

        // Act
        var upload = Upload.Create(userId, fileName, contentType, fileSize, totalParts);

        // Assert
        upload.Id.Should().NotBeEmpty();
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
    public void StartProcessing_WhenUploading_ShouldChangeStatusToProcessing()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);
        upload.StartUploading();

        // Act
        upload.StartProcessing();

        // Assert
        upload.Status.Should().Be(UploadStatus.Processing);
    }

    [Fact]
    public void StartProcessing_WhenNotUploading_ShouldThrowException()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);

        // Act
        var act = () => upload.StartProcessing();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot start processing from status Pending");
    }

    [Fact]
    public void MarkAsCompleted_WhenProcessing_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);
        upload.StartUploading();
        upload.StartProcessing();

        // Act
        upload.MarkAsCompleted();

        // Assert
        upload.Status.Should().Be(UploadStatus.Completed);
        upload.CompletedAt.Should().NotBeNull();
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
    }

    [Fact]
    public void SetMultipartUploadId_ShouldSetIdAndUpdateTimestamp()
    {
        // Arrange
        var upload = Upload.Create("user-123", "test.mp4", "video/mp4", 1024, 1);
        var multipartUploadId = "upload-id-123";

        // Act
        upload.SetMultipartUploadId(multipartUploadId);

        // Assert
        upload.MultipartUploadId.Should().Be(multipartUploadId);
        upload.UpdatedAt.Should().NotBeNull();
    }
}
