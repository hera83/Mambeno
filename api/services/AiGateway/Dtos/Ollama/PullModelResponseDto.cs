using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Ollama;

public class PullModelResponseDto
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
