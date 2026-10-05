using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.KnowledgeBase;

public class SearchResponseDto
{
    [JsonPropertyName("matches")]
    public List<ChunkMatchResponseDto>? Matches { get; set; }
}
