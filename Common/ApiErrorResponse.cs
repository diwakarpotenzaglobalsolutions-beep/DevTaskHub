using System.Text.Json.Serialization;

namespace DevTaskHubBackend.Models;

public sealed class ApiErrorResponse
{
    public int StatusCode { get; init; }

    public string Message { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public object? Data { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }
}

public static class HttpStatusMessages
{
    public static string Get(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "The request is invalid.",
        StatusCodes.Status401Unauthorized => "Authentication is required.",
        StatusCodes.Status403Forbidden => "You do not have permission to perform this action.",
        StatusCodes.Status404NotFound => "The requested resource was not found.",
        StatusCodes.Status405MethodNotAllowed => "This HTTP method is not allowed.",
        StatusCodes.Status406NotAcceptable => "The requested format is not acceptable.",
        StatusCodes.Status408RequestTimeout => "The request timed out.",
        StatusCodes.Status409Conflict => "The request conflicts with the current data.",
        StatusCodes.Status410Gone => "The requested resource is no longer available.",
        StatusCodes.Status411LengthRequired => "Content length is required.",
        StatusCodes.Status412PreconditionFailed => "A precondition for this request failed.",
        StatusCodes.Status413PayloadTooLarge => "The request body is too large.",
        StatusCodes.Status414UriTooLong => "The request URI is too long.",
        StatusCodes.Status415UnsupportedMediaType => "The media type is not supported.",
        StatusCodes.Status422UnprocessableEntity => "The request could not be processed.",
        StatusCodes.Status429TooManyRequests => "Too many requests. Try again later.",
        StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
        StatusCodes.Status501NotImplemented => "This operation is not implemented.",
        StatusCodes.Status502BadGateway => "A downstream service failed.",
        StatusCodes.Status503ServiceUnavailable => "The service is temporarily unavailable.",
        StatusCodes.Status504GatewayTimeout => "A downstream service timed out.",
        _ => "The request could not be completed."
    };
}
