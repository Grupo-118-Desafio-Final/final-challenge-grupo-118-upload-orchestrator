using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using UploadsApi.Application.DTOs;
using UploadsApi.Application.Interfaces;
using UploadsApi.Domain.Entities;

namespace UploadsApi.Api.Tests;

public class UploadsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UploadsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Id", "test-user-123");
        _client.DefaultRequestHeaders.Add("X-Plan-Id", "test-plan-456");
    }

    [Fact]
    public async Task CreateUpload_WhenValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var request = new CreateUploadRequest("test-video.mp4", "video/mp4", 1024 * 1024 * 100, 10);

        // Act
        var response = await _client.PostAsJsonAsync("/api/uploads", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateUploadResponse>();
        result.Should().NotBeNull();
        result!.UploadId.Should().NotBeEmpty();
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task CreateUpload_WhenInvalidContentType_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateUploadRequest("test.txt", "text/plain", 1024, 1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/uploads", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUpload_WhenMissingUserId_ShouldReturn401Unauthorized()
    {
        // Arrange
        var clientWithoutUser = _factory.CreateClient();
        var request = new CreateUploadRequest("test.mp4", "video/mp4", 1024, 1);

        // Act
        var response = await clientWithoutUser.PostAsJsonAsync("/api/uploads", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUpload_WhenExists_ShouldReturn200Ok()
    {
        // Arrange
        var userId = "test-user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 1);

        _factory.UploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        var response = await _client.GetAsync($"/api/uploads/{upload.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
        result.Should().NotBeNull();
        result!.FileName.Should().Be("test.mp4");
    }

    [Fact]
    public async Task GetUpload_WhenNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        var uploadId = Guid.NewGuid();
        var userId = "test-user-123";

        _factory.UploadRepository.GetByIdAndUserIdAsync(uploadId, userId, Arg.Any<CancellationToken>())
            .Returns((Upload?)null);

        // Act
        var response = await _client.GetAsync($"/api/uploads/{uploadId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUploads_ShouldReturn200OkWithPagedResponse()
    {
        // Arrange
        var userId = "test-user-123";
        var uploads = new List<Upload>
        {
            Upload.Create(userId, "test1.mp4", "video/mp4", 1024, 1),
            Upload.Create(userId, "test2.mp4", "video/mp4", 2048, 1)
        };

        _factory.UploadRepository.GetByUserIdAsync(userId, 1, 10, Arg.Any<CancellationToken>())
            .Returns((uploads, 2));

        // Act
        var response = await _client.GetAsync("/api/uploads");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<UploadResponse>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task AbortUpload_WhenExists_ShouldReturn204NoContent()
    {
        // Arrange
        var userId = "test-user-123";
        var upload = Upload.Create(userId, "test.mp4", "video/mp4", 1024, 1);

        _factory.UploadRepository.GetByIdAndUserIdAsync(upload.Id, userId, Arg.Any<CancellationToken>())
            .Returns(upload);

        // Act
        var response = await _client.DeleteAsync($"/api/uploads/{upload.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
