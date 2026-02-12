using FluentValidation;
using UploadsApi.Application.DTOs;

namespace UploadsApi.Application.Validators;

public class CompleteUploadRequestValidator : AbstractValidator<CompleteUploadRequest>
{
    public CompleteUploadRequestValidator()
    {
        RuleFor(x => x.Parts)
            .NotEmpty().WithMessage("Parts list is required");

        RuleForEach(x => x.Parts)
            .ChildRules(part =>
            {
                part.RuleFor(p => p.PartNumber)
                    .GreaterThan(0).WithMessage("Part number must be greater than 0");

                // ETag is required for S3/MinIO but not for Azure Blob Storage
                // Azure uses block IDs derived from part numbers instead
            });
    }
}
