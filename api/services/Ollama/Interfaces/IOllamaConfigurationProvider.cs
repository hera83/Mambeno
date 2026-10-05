namespace api.Services.Ollama.Interfaces;

public interface IOllamaConfigurationProvider
{
    Task<OllamaSettings> GetActiveConfigurationAsync(CancellationToken cancellationToken = default);
}
