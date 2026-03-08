namespace UploadsApi.Domain.Exceptions;

public class MissingPlanIdException : UnauthorizedAccessException
{
    public MissingPlanIdException() : base("Plan ID not found in request headers")
    {
    }
}
