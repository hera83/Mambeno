using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Keys;

public class RolloverKeyResponseDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("lastRotatedAt")]
    public DateTime LastRotatedAt { get; set; }
}
