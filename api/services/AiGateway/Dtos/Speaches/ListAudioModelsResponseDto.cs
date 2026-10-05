using System.Text.Json.Serialization;

namespace api.Services.AiGateway.Dtos.Speaches;

public class ListAudioModelsResponseDto
{
    [JsonPropertyName("models")]
    public List<ModelDto>? Models { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }
}
