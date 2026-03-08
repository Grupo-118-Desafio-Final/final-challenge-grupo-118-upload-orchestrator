using System.Diagnostics.CodeAnalysis;

namespace UploadsApi.Domain.Exceptions;

[ExcludeFromCodeCoverage]
public class MissingUserIdException : UnauthorizedAccessException
{
    public MissingUserIdException() : base("User ID not found in request headers")
    {
    }
}
