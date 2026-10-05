using System.Text.Json;
using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Ollama;

public class OllamaToolFunctionDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public JsonElement? Parameters { get; set; }
}
