using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Ollama;

public class ListModelsResponseDto
{
    [JsonPropertyName("models")]
    public List<OllamaModelSummaryDto>? Models { get; set; }
}
