using System.Text.Json.Serialization;

namespace api.Services.Ollama.Dto;

public class OllamaRunningModelsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaRunningModelDto> Models { get; set; } = new();
}
