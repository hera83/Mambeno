using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.KnowledgeBase;

public class GroupResponseDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
