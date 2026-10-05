using System.Text.Json.Serialization;

namespace api.Services.Ollama.Dto;

public class OllamaTopLogProbDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("logprob")]
    public double LogProb { get; set; }
}
