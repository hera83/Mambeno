using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Ollama;

public class CopyModelRequestDto
{
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("destination")]
    public string? Destination { get; set; }
}
