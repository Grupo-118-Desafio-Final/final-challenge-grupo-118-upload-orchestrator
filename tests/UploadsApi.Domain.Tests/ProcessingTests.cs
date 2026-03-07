using FluentAssertions;
using UploadsApi.Domain.Entities;

namespace UploadsApi.Domain.Tests;

public class ProcessingTests
{
    [Fact]
    public void Create_ShouldCreateProcessingWithPendingStatus()
    {
        // Arrange
        var userId = "user-123";
        var objectKey = "uploads/user-123/20240101_video.mp4";

        // Act
        var processing = Processing.Create(userId, objectKey);

        // Assert
        processing.Id.Should().NotBeEmpty();
        processing.UserId.Should().Be(userId);
        processing.ObjectKey.Should().Be(objectKey);
        processing.Status.Should().Be("Pending");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var processing1 = Processing.Create("user-1", "key1");
        var processing2 = Processing.Create("user-2", "key2");

        // Assert
        processing1.Id.Should().NotBe(processing2.Id);
    }

    [Fact]
    public void Create_ShouldPreserveUserIdAndObjectKey()
    {
        // Arrange
        var userId = "user-abc";
        var objectKey = "uploads/user-abc/20240101120000_abc123_video.mp4";

        // Act
        var processing = Processing.Create(userId, objectKey);

        // Assert
        processing.UserId.Should().Be(userId);
        processing.ObjectKey.Should().Be(objectKey);
    }
}
