using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.KnowledgeBase;

public class CreateGroupRequestDto
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
