namespace UploadsApi.Domain.Entities;

public class Processing
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string ObjectKey { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;

    private Processing() { }

    public static Processing Create(string userId, string objectKey)
    {
        return new Processing
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ObjectKey = objectKey,
            Status = "Pending"
        };
    }
}
