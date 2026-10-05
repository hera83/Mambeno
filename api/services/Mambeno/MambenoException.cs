using System.Net;

namespace api.Services.Mambeno;

public class MambenoException : Exception
{
    public HttpStatusCode StatusCode { get; }

    /// <summary>WordPress' fejlkode, fx "rest_post_invalid_id" eller "rest_post_invalid_page_number".</summary>
    public string? ErrorCode { get; }

    public MambenoException(HttpStatusCode statusCode, string message, string? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
