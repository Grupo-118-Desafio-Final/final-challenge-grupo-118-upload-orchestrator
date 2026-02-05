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

                part.RuleFor(p => p.ETag)
                    .NotEmpty().WithMessage("ETag is required for each part");
            });
    }
}
