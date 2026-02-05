using FluentAssertions;
using UploadsApi.Application.DTOs;
using UploadsApi.Application.Validators;

namespace UploadsApi.Application.Tests;

public class CreateUploadRequestValidatorTests
{
    private readonly CreateUploadRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WhenValid_ShouldReturnSuccess()
    {
        // Arrange
        var request = new CreateUploadRequest("test.mp4", "video/mp4", 1024 * 1024, 1);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WhenFileNameEmpty_ShouldReturnError(string? fileName)
    {
        // Arrange
        var request = new CreateUploadRequest(fileName!, "video/mp4", 1024, 1);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task Validate_WhenFileNameTooLong_ShouldReturnError()
    {
        // Arrange
        var longFileName = new string('a', 256) + ".mp4";
        var request = new CreateUploadRequest(longFileName, "video/mp4", 1024, 1);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task Validate_WhenContentTypeInvalid_ShouldReturnError()
    {
        // Arrange
        var request = new CreateUploadRequest("test.txt", "text/plain", 1024, 1);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }

    [Theory]
    [InlineData("video/mp4")]
    [InlineData("video/mpeg")]
    [InlineData("video/quicktime")]
    [InlineData("video/x-msvideo")]
    [InlineData("video/webm")]
    [InlineData("video/x-matroska")]
    public async Task Validate_WhenContentTypeValid_ShouldReturnSuccess(string contentType)
    {
        // Arrange
        var request = new CreateUploadRequest("test.mp4", contentType, 1024, 1);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenFileSizeZero_ShouldReturnError()
    {
        // Arrange
        var request = new CreateUploadRequest("test.mp4", "video/mp4", 0, 1);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileSize");
    }

    [Fact]
    public async Task Validate_WhenFileSizeTooLarge_ShouldReturnError()
    {
        // Arrange
        var maxSize = 5L * 1024 * 1024 * 1024 + 1; // > 5 GB
        var request = new CreateUploadRequest("test.mp4", "video/mp4", maxSize, 1);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileSize");
    }

    [Fact]
    public async Task Validate_WhenTotalPartsZero_ShouldReturnError()
    {
        // Arrange
        var request = new CreateUploadRequest("test.mp4", "video/mp4", 1024, 0);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalParts");
    }

    [Fact]
    public async Task Validate_WhenTotalPartsTooMany_ShouldReturnError()
    {
        // Arrange
        var request = new CreateUploadRequest("test.mp4", "video/mp4", 1024, 10001);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalParts");
    }
}
