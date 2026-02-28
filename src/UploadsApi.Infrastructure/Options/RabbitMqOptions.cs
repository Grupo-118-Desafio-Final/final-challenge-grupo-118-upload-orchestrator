namespace UploadsApi.Infrastructure.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Uri { get; set; } = "amqp://guest:guest@localhost/";
    public string ExchangeName { get; set; } = "uploads";
    public string ExchangeType { get; set; } = "topic";
}
