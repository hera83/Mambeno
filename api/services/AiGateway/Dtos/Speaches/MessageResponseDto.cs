using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Speaches;

public class MessageResponseDto
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
