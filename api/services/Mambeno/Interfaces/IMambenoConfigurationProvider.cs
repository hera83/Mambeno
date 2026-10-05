namespace api.Services.Mambeno.Interfaces;

public interface IMambenoConfigurationProvider
{
    Task<MambenoSettings> GetActiveConfigurationAsync(CancellationToken cancellationToken = default);
}
