using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Speaches;

public class EmbeddingUsageDto
{
    [JsonPropertyName("promptTokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; }
}
