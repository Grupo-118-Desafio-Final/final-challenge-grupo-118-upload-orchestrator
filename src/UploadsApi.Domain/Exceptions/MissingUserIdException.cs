namespace UploadsApi.Domain.Exceptions;

public class MissingUserIdException : UnauthorizedAccessException
{
    public MissingUserIdException() : base("User ID not found in request headers")
    {
    }
}
