using System.Text.Json.Serialization;

namespace api.Services.Ollama.Dto;

public class OllamaDeleteRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}
