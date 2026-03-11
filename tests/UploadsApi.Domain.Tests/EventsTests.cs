using FluentAssertions;
using MongoDB.Bson;
using UploadsApi.Domain.Events;

namespace UploadsApi.Domain.Tests;

public class EventsTests
{
    [Fact]
    public void UploadFailedEvent_ShouldStoreAllProperties()
    {
        // Arrange
        var uploadId = ObjectId.GenerateNewId().ToString();
        var userId = "user-123";
        var fileName = "video.mp4";
        var errorMessage = "Processing failed";
        var failedAt = DateTime.UtcNow;

        // Act
        var evt = new UploadFailedEvent(uploadId, userId, fileName, errorMessage, failedAt);

        // Assert
        evt.UploadId.Should().Be(uploadId);
        evt.UserId.Should().Be(userId);
        evt.FileName.Should().Be(fileName);
        evt.ErrorMessage.Should().Be(errorMessage);
        evt.FailedAt.Should().Be(failedAt);
    }

    [Fact]
    public void UploadFailedEvent_ShouldSupportValueEquality()
    {
        // Arrange
        var uploadId = ObjectId.GenerateNewId().ToString();
        var failedAt = DateTime.UtcNow;

        var evt1 = new UploadFailedEvent(uploadId, "user-1", "video.mp4", "error", failedAt);
        var evt2 = new UploadFailedEvent(uploadId, "user-1", "video.mp4", "error", failedAt);

        // Assert
        evt1.Should().Be(evt2);
    }

    [Fact]
    public void VideoUploadedEvent_ShouldStoreAllProperties()
    {
        // Arrange
        var userId = "user-123";
        var planId = "plan-basic";
        var processingId = ObjectId.GenerateNewId().ToString();
        var blobUrl = "https://storage.example.com/bucket/video.mp4";
        var eventAt = DateTime.UtcNow;

        // Act
        var evt = new VideoUploadedEvent(userId, planId, processingId, blobUrl, eventAt);

        // Assert
        evt.UserId.Should().Be(userId);
        evt.PlanId.Should().Be(planId);
        evt.ProcessingId.Should().Be(processingId);
        evt.BlobUrl.Should().Be(blobUrl);
        evt.EventAt.Should().Be(eventAt);
    }

    [Fact]
    public void VideoUploadedEvent_ShouldSupportValueEquality()
    {
        // Arrange
        var processingId = ObjectId.GenerateNewId().ToString();
        var eventAt = DateTime.UtcNow;

        var evt1 = new VideoUploadedEvent("user-1", "plan-1", processingId, "https://url", eventAt);
        var evt2 = new VideoUploadedEvent("user-1", "plan-1", processingId, "https://url", eventAt);

        // Assert
        evt1.Should().Be(evt2);
    }

    [Fact]
    public void VideoUploadedEvent_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var eventAt = DateTime.UtcNow;
        var evt1 = new VideoUploadedEvent("user-1", "plan-1", ObjectId.GenerateNewId().ToString(), "https://url1", eventAt);
        var evt2 = new VideoUploadedEvent("user-2", "plan-2", ObjectId.GenerateNewId().ToString(), "https://url2", eventAt);

        // Assert
        evt1.Should().NotBe(evt2);
    }
}
