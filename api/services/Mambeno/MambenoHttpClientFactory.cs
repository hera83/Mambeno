namespace api.Services.Mambeno;

public class MambenoHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MambenoHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient Create(string baseUrl, string userAgent, int requestTimeoutSeconds)
    {
        var client = _httpClientFactory.CreateClient("Mambeno");
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds > 0 ? requestTimeoutSeconds : 60);

        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

        return client;
    }
}
