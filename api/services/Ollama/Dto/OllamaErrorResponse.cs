using System.Text.Json.Serialization;

namespace api.Services.Ollama.Dto;

public class OllamaErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
