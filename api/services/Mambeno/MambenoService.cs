using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Services.Mambeno.Dtos.Errors;
using api.Services.Mambeno.Interfaces;

namespace api.Services.Mambeno;

public partial class MambenoService : IMambenoService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new LenientNullableDoubleJsonConverter() }
    };

    // WordPress-fejlkoder der betyder "findes ikke (længere) offentligt". Alt andet - fx en 403 fra en
    // firewall med en HTML-side - tolkes IKKE som at indholdet er væk, så vi aldrig sletter på en fejl.
    private static readonly HashSet<string> NotFoundErrorCodes =
    [
        "rest_post_invalid_id",
        "rest_term_invalid",
        "rest_forbidden",
        "rest_cannot_read"
    ];

    // Statisk så alle kald til Mambeno i hele appen deler samme kø: højst ét kald ad gangen og altid
    // mindst DelayBetweenRequestsMilliseconds mellem to kald, uanset hvor mange scopes der kører.
    private static readonly SemaphoreSlim RequestGate = new(1, 1);
    private static DateTime _nextRequestAllowedAtUtc = DateTime.MinValue;

    private readonly IMambenoConfigurationProvider _configurationProvider;
    private readonly MambenoHttpClientFactory _httpClientFactory;
    private readonly ILogger<MambenoService> _logger;

    public MambenoService(
        IMambenoConfigurationProvider configurationProvider,
        MambenoHttpClientFactory httpClientFactory,
        ILogger<MambenoService> logger)
    {
        _configurationProvider = configurationProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private async Task<(HttpClient Client, MambenoSettings Settings)> CreateClientAsync(CancellationToken cancellationToken)
    {
        var settings = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        var client = _httpClientFactory.Create(settings.BaseUrl, settings.UserAgent, settings.RequestTimeoutSeconds);
        return (client, settings);
    }

    private async Task<T> GetJsonAsync<T>(HttpClient client, MambenoSettings settings, string endpoint, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(client, settings, endpoint, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<T>(response, cancellationToken);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            return JsonSerializer.Deserialize<T>(payload, JsonOptions)
                   ?? throw new MambenoException(HttpStatusCode.BadGateway, "Mambeno returnerede et tomt svar.");
        }
        catch (JsonException ex)
        {
            throw new MambenoException(HttpStatusCode.BadGateway, "Kunne ikke parse svaret fra Mambeno (forventede JSON).", innerException: ex);
        }
    }

    /// <summary>
    /// Sender et GET gennem den fælles kø og prøver igen ved midlertidige fejl. Ikke-midlertidige
    /// fejlsvar (fx 404) returneres som de er, så kalderen selv kan tage stilling til dem.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(HttpClient client, MambenoSettings settings, string endpoint, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage? response = null;
            Exception? failure = null;

            await RequestGate.WaitAsync(cancellationToken);
            try
            {
                var wait = _nextRequestAllowedAtUtc - DateTime.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, cancellationToken);
                }

                response = await client.GetAsync(endpoint, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                failure = ex;
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // HttpClient.Timeout udløst (ikke vores egen annullering).
                failure = ex;
            }
            finally
            {
                _nextRequestAllowedAtUtc = DateTime.UtcNow.AddMilliseconds(settings.DelayBetweenRequestsMilliseconds);
                RequestGate.Release();
            }

            if (response is not null && !IsTransient(response.StatusCode))
            {
                return response;
            }

            if (attempt >= settings.MaxRetries)
            {
                if (response is not null)
                {
                    return response;
                }

                throw new MambenoException(
                    HttpStatusCode.BadGateway,
                    $"Kunne ikke kontakte Mambeno ({endpoint}) efter {attempt + 1} forsøg: {failure?.Message}",
                    innerException: failure);
            }

            var retryDelay = response?.Headers.RetryAfter?.Delta
                             ?? TimeSpan.FromSeconds(settings.RetryBaseDelaySeconds * Math.Pow(2, attempt));
            _logger.LogWarning(
                "Midlertidig fejl fra Mambeno ved {Endpoint} ({Reason}); prøver igen om {RetryDelaySeconds} sek. (forsøg {Attempt}/{MaxRetries})",
                endpoint,
                response is not null ? $"status {(int)response.StatusCode}" : failure?.Message,
                (int)retryDelay.TotalSeconds,
                attempt + 1,
                settings.MaxRetries);

            response?.Dispose();
            await Task.Delay(retryDelay, cancellationToken);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)statusCode >= 500;

    /// <summary>
    /// True hvis svaret er WordPress' egen "findes ikke"-fejl. Bruges før sletning, så kun et entydigt
    /// svar fra WordPress (og ikke fx en firewall- eller vedligeholdelsesside) kan få os til at slette.
    /// </summary>
    private static async Task<bool> IsWordPressNotFoundAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is not (HttpStatusCode.NotFound or HttpStatusCode.Gone or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
        {
            return false;
        }

        var error = await TryReadErrorAsync(response, cancellationToken);
        return error?.Code is not null && NotFoundErrorCodes.Contains(error.Code);
    }

    private static async Task<ErrorResponseDto?> TryReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ErrorResponseDto>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await TryReadErrorAsync(response, cancellationToken);
        var message = !string.IsNullOrWhiteSpace(error?.Message)
            ? error.Message
            : $"Mambeno request fejlede med status {(int)response.StatusCode}.";

        throw new MambenoException(response.StatusCode, message, error?.Code);
    }
}
