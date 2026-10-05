using api.Services.Mambeno.Interfaces;

namespace api.Services.Mambeno;

public class MambenoConfigurationProvider : IMambenoConfigurationProvider
{
    private readonly IConfiguration _configuration;

    public MambenoConfigurationProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<MambenoSettings> GetActiveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new MambenoSettings();
        var settings = new MambenoSettings
        {
            BaseUrl = NonEmpty(_configuration["Mambeno:BaseUrl"]) ?? defaults.BaseUrl,
            UserAgent = NonEmpty(_configuration["Mambeno:UserAgent"]) ?? defaults.UserAgent,
            RequestTimeoutSeconds = PositiveInt("Mambeno:RequestTimeoutSeconds") ?? defaults.RequestTimeoutSeconds,
            DelayBetweenRequestsMilliseconds = PositiveInt("Mambeno:DelayBetweenRequestsMilliseconds") ?? defaults.DelayBetweenRequestsMilliseconds,
            PageSize = Math.Min(PositiveInt("Mambeno:PageSize") ?? defaults.PageSize, 100),
            MaxRetries = int.TryParse(_configuration["Mambeno:MaxRetries"], out var retries) && retries >= 0 ? retries : defaults.MaxRetries,
            RetryBaseDelaySeconds = PositiveInt("Mambeno:RetryBaseDelaySeconds") ?? defaults.RetryBaseDelaySeconds,
        };

        return Task.FromResult(settings);
    }

    private int? PositiveInt(string key)
        => int.TryParse(_configuration[key], out var value) && value > 0 ? value : null;

    private static string? NonEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
