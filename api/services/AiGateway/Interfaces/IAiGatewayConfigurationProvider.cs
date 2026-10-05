namespace api.Services.AiGateway.Interfaces;

public interface IAiGatewayConfigurationProvider
{
    Task<AiGatewaySettings> GetActiveConfigurationAsync(CancellationToken cancellationToken = default);
}
