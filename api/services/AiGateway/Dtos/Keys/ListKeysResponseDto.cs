using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Keys;

public class ListKeysResponseDto
{
    [JsonPropertyName("keys")]
    public List<KeyResponseDto>? Keys { get; set; }
}
