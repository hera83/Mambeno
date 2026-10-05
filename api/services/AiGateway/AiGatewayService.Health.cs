using api.Services.AiGateway.Dtos.Health;

namespace api.Services.AiGateway;

public partial class AiGatewayService
{
    public async Task<HealthResponseDto> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await GetAsync<HealthResponseDto>(client, "Health/Check", cancellationToken);
    }
}
