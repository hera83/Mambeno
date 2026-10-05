using api.Services.AiGateway;
using api.Services.Mambeno;
using api.Services.Ollama;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.Services.ErrorHandling;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, detail) = exception switch
        {
            AiGatewayException aiGatewayException => ((int)aiGatewayException.StatusCode, aiGatewayException.Message),
            OllamaException ollamaException => ((int)ollamaException.StatusCode, ollamaException.Message),
            MambenoException mambenoException => ((int)mambenoException.StatusCode, mambenoException.Message),
            _ => (StatusCodes.Status500InternalServerError, environment.IsDevelopment() ? exception.Message : null)
        };

        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Detail = detail
            }
        });
    }
}
