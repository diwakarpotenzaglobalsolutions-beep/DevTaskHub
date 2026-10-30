namespace DevTaskHubBackend.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(int statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
