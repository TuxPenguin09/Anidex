namespace Anidex.Services;

/// <summary>
/// Thrown by <see cref="MalService"/> when the Jikan API returns a non-success
/// status code (4xx/5xx). Carries the upstream status so the UI can surface it
/// instead of a generic error string.
/// </summary>
public class MalApiException : Exception
{
    public int StatusCode { get; }

    public MalApiException(int statusCode, string message, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}