using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Ollama;

public class EmbedResponseDto
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("embeddings")]
    public List<List<double>>? Embeddings { get; set; }

    [JsonPropertyName("totalDuration")]
    public long? TotalDuration { get; set; }

    [JsonPropertyName("loadDuration")]
    public long? LoadDuration { get; set; }

    [JsonPropertyName("promptEvalCount")]
    public int? PromptEvalCount { get; set; }
}
