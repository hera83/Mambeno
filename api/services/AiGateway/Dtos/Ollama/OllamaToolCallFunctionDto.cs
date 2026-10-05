using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Ollama;

public class OllamaToolCallFunctionDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public Dictionary<string, object?>? Arguments { get; set; }
}
