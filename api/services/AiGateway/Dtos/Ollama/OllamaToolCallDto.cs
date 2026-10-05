using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Ollama;

public class OllamaToolCallDto
{
    [JsonPropertyName("function")]
    public OllamaToolCallFunctionDto? Function { get; set; }
}
