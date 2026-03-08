using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using UploadsApi.Application.DTOs;

namespace UploadsApi.Application.Validators;

[ExcludeFromCodeCoverage]
public class CreateUploadRequestValidator : AbstractValidator<CreateUploadRequest>
{
    private static readonly string[] AllowedContentTypes =
    [
        "video/mp4",
        "video/mpeg",
        "video/quicktime",
        "video/x-msvideo",
        "video/webm",
        "video/x-matroska"
    ];

    private const long MaxFileSize = 5L * 1024 * 1024 * 1024; // 5 GB
    private const int MaxParts = 10000;

    public CreateUploadRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required")
            .MaximumLength(255).WithMessage("File name cannot exceed 255 characters")
            .Must(BeAValidFileName).WithMessage("File name contains invalid characters");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required")
            .Must(BeAnAllowedContentType).WithMessage($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File size must be greater than 0")
            .LessThanOrEqualTo(MaxFileSize).WithMessage($"File size cannot exceed {MaxFileSize / (1024 * 1024 * 1024)} GB");

        RuleFor(x => x.TotalParts)
            .GreaterThan(0).WithMessage("Total parts must be greater than 0")
            .LessThanOrEqualTo(MaxParts).WithMessage($"Total parts cannot exceed {MaxParts}");
    }

    private static bool BeAValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var invalidChars = Path.GetInvalidFileNameChars();
        return !fileName.Any(c => invalidChars.Contains(c));
    }

    private static bool BeAnAllowedContentType(string contentType)
    {
        return AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }
}
