using System.Net;
using System.Text.Json;

namespace UploadsApi.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "An unexpected error occurred");
        }
        else
        {
            _logger.LogWarning(exception, "A handled exception occurred: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new
        {
            type = GetProblemType(statusCode),
            title = GetProblemTitle(statusCode),
            status = (int)statusCode,
            detail = message,
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);

        await context.Response.WriteAsync(json);
    }

    private static string GetProblemType(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        HttpStatusCode.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };

    private static string GetProblemTitle(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => "Not Found",
        HttpStatusCode.BadRequest => "Bad Request",
        HttpStatusCode.Unauthorized => "Unauthorized",
        _ => "Internal Server Error"
    };
}
