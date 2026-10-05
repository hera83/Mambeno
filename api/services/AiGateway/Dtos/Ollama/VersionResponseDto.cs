using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Ollama;

public class VersionResponseDto
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}
