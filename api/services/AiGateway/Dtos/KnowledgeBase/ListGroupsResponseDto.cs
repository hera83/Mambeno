using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.KnowledgeBase;

public class ListGroupsResponseDto
{
    [JsonPropertyName("groups")]
    public List<GroupResponseDto>? Groups { get; set; }
}
