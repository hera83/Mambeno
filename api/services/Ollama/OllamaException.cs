using System.Net;

namespace api.Services.Ollama;

public class OllamaException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public OllamaException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
