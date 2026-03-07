using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UploadsApi.Application.DTOs;
using UploadsApi.Application.Services;

namespace UploadsApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    private readonly IUploadService _uploadService;
    private readonly IValidator<CreateUploadRequest> _createUploadValidator;

    public UploadsController(
        IUploadService uploadService,
        IValidator<CreateUploadRequest> createUploadValidator)
    {
        _uploadService = uploadService;
        _createUploadValidator = createUploadValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUpload(
        [FromBody] CreateUploadRequest request,
        [FromHeader(Name = "X-User-Id")] string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in request headers");

        var validationResult = await _createUploadValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        var response = await _uploadService.CreateUploadAsync(userId, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetUpload),
            new { id = response.UploadId },
            response);
    }

    [HttpGet("{id:guid}/presigned-urls")]
    [ProducesResponseType(typeof(PresignedUrlsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPresignedUrls(
        Guid id,
        [FromHeader(Name = "X-User-Id")] string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in request headers");

        var response = await _uploadService.GetPresignedUrlsAsync(id, userId, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteUpload(
        Guid id,
        [FromHeader(Name = "X-User-Id")] string userId,
        [FromHeader(Name = "X-Plan-Id")] string planId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in request headers");

        if (string.IsNullOrEmpty(planId))
            throw new UnauthorizedAccessException("Plan ID not found in request headers");

        await _uploadService.CompleteUploadAsync(id, userId, planId, cancellationToken);

        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UploadResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUploads(
        [FromHeader(Name = "X-User-Id")] string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in request headers");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var response = await _uploadService.GetUploadsAsync(userId, page, pageSize, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUpload(
        Guid id,
        [FromHeader(Name = "X-User-Id")] string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in request headers");

        var response = await _uploadService.GetUploadAsync(id, userId, cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AbortUpload(
        Guid id,
        [FromHeader(Name = "X-User-Id")] string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found in request headers");

        await _uploadService.AbortUploadAsync(id, userId, cancellationToken);

        return NoContent();
    }
}
