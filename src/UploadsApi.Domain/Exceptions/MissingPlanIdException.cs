using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Domain.Exceptions;

[ExcludeFromCodeCoverage]
public class MissingPlanIdException : UnauthorizedAccessException
{
    public MissingPlanIdException() : base("Plan ID not found in request headers")
    {
    }
}
