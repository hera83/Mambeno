using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.KnowledgeBase;

public class ListDocumentsResponseDto
{
    [JsonPropertyName("documents")]
    public List<DocumentResponseDto>? Documents { get; set; }
}
