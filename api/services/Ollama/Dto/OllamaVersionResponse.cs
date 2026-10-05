using System.Text.Json.Serialization;

namespace api.Services.Ollama.Dto;

public class OllamaVersionResponse
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
