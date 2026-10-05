using System.Text.Json.Serialization;

namespace api.Services.Ollama.Dto;

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelSummaryDto> Models { get; set; } = new();
}
