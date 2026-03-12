using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UploadsApi.Application.DTOs;
using UploadsApi.Application.Services;
using UploadsApi.Domain.Exceptions;

namespace UploadsApi.Api.Controllers;

/// <summary>
///
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[ExcludeFromCodeCoverage]
public class UploadsController : ControllerBase
{
    private readonly IUploadService _uploadService;
    private readonly IValidator<CreateUploadRequest> _createUploadValidator;

    /// <summary>
    ///
    /// </summary>
    /// <param name="uploadService"></param>
    /// <param name="createUploadValidator"></param>
    public UploadsController(
        IUploadService uploadService,
        IValidator<CreateUploadRequest> createUploadValidator)
    {
        _uploadService = uploadService;
        _createUploadValidator = createUploadValidator;
    }

    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new MissingUserIdException();
        return userId;
    }

    private string GetPlanId()
    {
        var planId = User.FindFirstValue("X-Plan-id");
        if (string.IsNullOrEmpty(planId))
            throw new MissingPlanIdException();
        return planId;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUpload(
        [FromBody] CreateUploadRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

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

    /// <summary>
    ///
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id}/presigned-urls")]
    [ProducesResponseType(typeof(PresignedUrlsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPresignedUrls(
        string id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _uploadService.GetPresignedUrlsAsync(id, userId, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteUpload(
        string id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var planId = GetPlanId();

        await _uploadService.CompleteUploadAsync(id, userId, planId, cancellationToken);

        return NoContent();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UploadResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUploads(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var response = await _uploadService.GetUploadsAsync(userId, page, pageSize, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUpload(
        string id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _uploadService.GetUploadAsync(id, userId, cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AbortUpload(
        string id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        await _uploadService.AbortUploadAsync(id, userId, cancellationToken);

        return NoContent();
    }
}
